# Introduction to the UXML serialization system

Greetings from the UI Toolkit team.
We've been attentively listening to our users' feedback, and one recurring concern that's been raised is the substantial amount of boilerplate code required when crafting your own custom elements. We are excited to introduce a series of enhancements aimed at streamlining your workflow, making it faster and more intuitive to create custom visual elements, while seamlessly incorporating the familiar Unity features you rely on, such as Serialization, Property drawers, decorators, and more.

## Comparison between the old and new system

Here's a comparison between the conventional custom control authoring process and our revamped workflow, using the example of creating a Progress bar component. Please note that this demonstration focuses solely on attribute authoring, as the behavioral aspects are not included.

```c#
using UnityEngine.UIElements;

public class ProgressBar : VisualElement
{
    public new class UxmlFactory : UxmlFactory<ProgressBar, UxmlTraits> { }

    public new class UxmlTraits : BindableElement.UxmlTraits
    {
        UxmlFloatAttributeDescription m_LowValue = new UxmlFloatAttributeDescription { name = "low-value", defaultValue = 0 };
        UxmlFloatAttributeDescription m_HighValue = new UxmlFloatAttributeDescription { name = "high-value", defaultValue = 100 };
        UxmlFloatAttributeDescription m_Value = new UxmlFloatAttributeDescription { name = "value", defaultValue = 0 };
        UxmlStringAttributeDescription m_Title = new UxmlStringAttributeDescription() { name = "title", defaultValue = string.Empty };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var bar = ve as ProgressBar;
            bar.lowValue = m_LowValue.GetValueFromBag(bag, cc);
            bar.highValue = m_HighValue.GetValueFromBag(bag, cc);
            bar.value = m_Value.GetValueFromBag(bag, cc);
            bar.title = m_Title.GetValueFromBag(bag, cc);
        }
    }

    public string title { get; set; }

    public float lowValue { get; set; }

    public float highValue { get; set; }

    public float value { get; set; }
}
```

In the example above, you can see that we have to create a **UxmlTraits** class. This class defines the attributes we want to include in the UXML element. For each attribute, we specify:

- Its type
- Default value
- The name it will have as a UXML attribute

Then, we have to get the value for each attribute and assign it to our element. We also need to add a **UxmlFactory**, which is what we use to create instances of the element.

Lets take a look at the new workflow:

```c#
[UxmlElement]
public partial class ProgressBar : VisualElement
{
    [UxmlAttribute]
    public string title { get; set; }

    [UxmlAttribute]
    public float lowValue { get; set; }

    [UxmlAttribute]
    public float highValue { get; set; } = 100;

    [UxmlAttribute]
    public float value { get; set; }
}
```

Here is what's changed:

- To declare a custom control, we now employ the [UxmlElement](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/UIElements.UxmlElementAttribute.html) attribute.
- We've introduced the requirement to mark the class as **partial** (details on this coming up).
- Each attribute now only requires the [UxmlAttribute](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/UIElements.UxmlAttributeAttribute.html) attribute – no more **UxmlTraits**.
- The attribute name is automatically derived from the property name, although you can also specify a custom name within the attribute arguments.

In both the above examples the UXML is the same:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ProgressBar title="My Progress bar" low-value="0" high-value="1" value="0.5" />
</ui:UXML>
```

As of now, when an element lacks the **UxmlElement** attribute, Unity resorts solely to the **UxmlTraits** and **UxmlFactory** systems for serialization across the entire class hierarchy. It's important to underline that we uphold a unified approach, employing a single system for serialization per visual element. While it's acceptable for both systems to be used when serializing a UXML file containing multiple elements, we do not mix them when processing a single element. However, it's crucial to be aware that we have plans to eventually phase out these older systems. Hence, we strongly advise transitioning to the new system for any future development efforts.

## Attribute Converters

The **UxmlAttribute** serves as a marker, signaling that the field or property is linked to a UXML attribute. Unity will automatically handle the conversion of values to and from UXML attribute strings as needed. This conversion is facilitated through the use of [UxmlAttributeConverter](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/UIElements.UxmlAttributeConverter_1.html). What's exciting is that you can now create custom converters to accommodate your specific data types.

Lets take a look at an example:

```c#
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

[Serializable]
public class Person
{
    public string name;
    public int age;
    public string nationality;
}

[UxmlElement]
public partial class Department : VisualElement
{
    [UxmlAttribute]
    public string name;

    [UxmlAttribute]
    public Person manager;

    [UxmlAttribute]
    public List<Person> employees;
}
```

In this scenario, we've introduced a custom class called `Person` with three fields. To make it compatible as a UXML attribute, Unity requires a way to convert it to and from a string. If you were to create something like this and attempt to edit it in the UI Builder, you'd encounter an error message like the one below:

> \[UxmlElement\] 'Department' has a \[UxmlAttribute\] 'manager' of an unknown type 'Person'.
> To fix this error define a custom UxmlAttributeConverter\<Person\>.

We can define a custom converter for our `Person` class like so:

```c#
public class PersonConverter : UxmlAttributeConverter<Person>
{
    const char k_Separator = ':';

    public override string ToString(Person value)
    {
        return $"{value.name}{k_Separator}{value.age}{k_Separator}{value.nationality}";
    }

    public override Person FromString(string value)
    {
        var person = new Person();
        var split = value.Split(k_Separator);
        if (split.Length == 3)
        {
            person.name = split[0];
            person.age = int.Parse(split[1]);
            person.nationality = split[2];
        }
        return person;
    }
}
```

Within our converter, we combine the three values into a single string format, using the pattern `[name]:[age]:[nationality]`. It's important to note that we've opted for the colon (:) as our separator, steering clear of the comma (,). This choice is deliberate to avoid conflicts, particularly when dealing with lists and arrays, which utilize commas (,) for string conversions. When supporting a list of `Person` instances, we must steer clear of the comma to prevent any clashes.

This is how UXML for our Department element would look:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <Department name="Dunder Mifflin" manager="Michael Scott:58:USA" employees="Dwight Schrute:53:USA,Jim Halpert:44:USA" />
</ui:UXML>
```

> Note the comma(,) used to separate the list values. We strongly advise you to avoid using commas (,) in your string converters and their generated content.

A full list of the types that we already support converters for can be found in the [scripting docs](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/UIElements.UxmlAttributeConverter_1.html).

## UxmlObjects

We've observed that while we can extend support for custom data types by defining a **UxmlAttributeConverter**, this approach has its limitations. As your data types become more intricate or when you're dealing with extensive lists, the generated strings can quickly grow convoluted and unwieldy. The notion of a **UxmlObject**, which is essentially an UXML element capable of being a child within a VisualElement, has already been a part of UI Toolkit. If you have ever authored a **MultiColumnListView** or **MultiColumnTreeView** you may have noticed them:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:MultiColumnListView name="listView" fixed-item-height="50">
        <ui:Columns>
            <ui:Column name="column1" title="Column 1" width="100"/>
            <ui:Column name="column2" title="Column 2" width="100"/>
            <ui:Column name="column3" title="Column 3" width="100"/>
        </ui:Columns>
    </ui:MultiColumnListView>
</ui:UXML>
```

The **Columns** and **Column** elements are prime examples of what we term as **UxmlObjects**. We've recently enhanced this feature, making it accessible for defining your own **UxmlObjects**. The process closely mirrors the way you define **UxmlElements**.

Lets take the previous example and convert it to use **UxmlObjects**:

```c#
[UxmlObject]
public partial class Person
{
    [UxmlAttribute]
    public string name;

    [UxmlAttribute]
    public int age;

    [UxmlAttribute]
    public string nationality;
}

[UxmlElement]
public partial class Department : VisualElement
{
    [UxmlObjectReference("manager")]
    public Person manager;

    [UxmlObjectReference("employees")]
    public List<Person> employees;
}
```

The `Person` class now resembles a custom element, adopting **UxmlAttribute** for attribute declaration and introducing the [UxmlObject](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/UIElements.UxmlObjectAttribute.html) attribute to signify its status as a **UxmlObject**.

A noteworthy shift is that, instead of employing **UxmlAttribute** for **UxmlObject** fields, we've introduced [UxmlObjectReference](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/UIElements.UxmlObjectReferenceAttribute.html). This new feature allows us to specify a name within **UxmlObjectReference**, indicating that the **UxmlObjects** will be parented to an element with that name. This addresses a previous limitation where all **UxmlObjects** were stored as direct children of the element, posing scalability issues if an element were to have multiple **UxmlObject** fields, as it becomes challenging to distinguish which **UxmlObjects** belonged to which field. If you leave the **UxmlObjectReference** name null or empty, it will revert to the previous behavior, which may be preferred when you have only one **UxmlObjectReferenceField**.

The UXML would now look like the following:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <Department name="Dunder Mifflin">
        <manager>
            <Person name="Michael Scott" age="58" nationality="USA">
        </manager>
        <employees>
            <Person name="Dwight Schrute" age="53" nationality="USA">
            <Person name="Jim Halpert" age="44" nationality="USA">
        </employees>
    </Department>
</ui:UXML>
```

One final noteworthy point to mention is that **UxmlObjectReferenceFields** can be used in conjunction with **interfaces**. However, it's important to note that you can only assign **UxmlObjects** that implement the specified interface. An example of using interfaces can be found in our [scripting docs](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/UIElements.UxmlObjectReferenceAttribute.html).

## How does it work?

Curious about how it all operates? For those of you who enjoy diving into the nitty-gritty, this section will provide an in-depth exploration of the inner workings.

We are using a [source generator](https://docs.unity3d.com/Manual/roslyn-analyzers.html). When your code compiles we detect the **UxmlElement** and **UxmlObject** attributes and add new code to those classes, this is why we require them to be marked as **partial**.
Are we simply generating the `UxmlTraits` and `UxmlFactory` through the source generator? Not quite. In fact, we've devised an innovative system that we've dubbed `UxmlSerializedData`.

If you're using certain IDEs, you might have the opportunity to peek at the generated code. Typically, pressing F12 on the partial class will reveal the generated code. While this feature is functional on Rider, it's worth noting that it's currently not available on Visual Studio.
Lets take a look at the generated code for our `ProgressBar` example.

```c#
public partial class ProgressBar
{
    [global::System.Runtime.CompilerServices.CompilerGenerated]
    [global::System.Serializable]
    public new class UxmlSerializedData : UnityEngine.UIElements.VisualElement.UxmlSerializedData
    {
        #pragma warning disable 649
        [SerializeField] private string title;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags title_UxmlAttributeFlags;
        [SerializeField] private float lowValue;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags lowValue_UxmlAttributeFlags;
        [SerializeField] private float highValue;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags highValue_UxmlAttributeFlags;
        [SerializeField] private float value;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags value_UxmlAttributeFlags;
        #pragma warning restore 649
        
        public override object CreateInstance() => new ProgressBar();
        public override void Deserialize(object obj)
        {
            base.Deserialize(obj);
            var e = (ProgressBar)obj;
            if (ShouldWriteAttributeValue(title_UxmlAttributeFlags))
                e.title = title;
            if (ShouldWriteAttributeValue(lowValue_UxmlAttributeFlags))
                e.lowValue = lowValue;
            if (ShouldWriteAttributeValue(highValue_UxmlAttributeFlags))
                e.highValue = highValue;
            if (ShouldWriteAttributeValue(value_UxmlAttributeFlags))
                e.value = value;
        }
    }
}
```

The **UxmlSerializedData** class takes on the roles of both the **UxmlTraits** and **UxmlFactory**. Every attribute is transformed into a [serialized field](https://docs.unity3d.com/ScriptReference/SerializeField.html) with an accompanying **UxmlAttributeFlags** field. The **Deserialize** method then handles the transfer of overridden attributes from these serialized fields to the source object. This operation is typically executed when importing UXML or making edits in the UI Builder. The **UxmlAttributeFlags** field is used to track which fields have been overridden in UXML and should be applied to the object, the other fields remain with their default values. We handle the **UxmlAttributeFlags** fields internally, you should never need to modify them yourself.

Now that we are using serialized fields this opens up a host of possibilities. We can support custom [property drawers](https://docs.unity3d.com/ScriptReference/PropertyDrawer.html), [decorators](https://docs.unity3d.com/ScriptReference/DecoratorDrawer.html), and other attributes such as [Header](https://docs.unity3d.com/ScriptReference/HeaderAttribute.html), [HideInInspector](https://docs.unity3d.com/ScriptReference/HideInInspector.html), [Range](https://docs.unity3d.com/ScriptReference/RangeAttribute.html) etc. You can see some examples of this in our [scripting docs](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/UIElements.UxmlAttributeAttribute.html).

To make this process smoother, we've implemented a mechanism to transfer any attributes from the source field to the serialized field. We've also relaxed some of the restrictions on our attributes to accommodate this functionality.
For instance, you can now apply attributes like **Range** directly to a property in addition to fields. This adjustment enables us to later transfer these attributes over to a serialized field in the generated **UxmlSerializedData** class.

The **UI Builder** now edits the elements **UxmlSerializedData** through [PropertyFields](https://docs.unity3d.com/ScriptReference/UIElements.PropertyField.html), resembling the Inspector approach.
When utilizing your custom types through the **AttributeConverter** approach, it is imperative to designate them as **Serializable**. This requirement ensures that they remain editable within the **UI Builder** attributes view and that their data remains intact during the serialization process."

The following is the generated code for our `Department` **AttributeConverter** example:

```c#
public partial class Department
{
    [global::System.Runtime.CompilerServices.CompilerGenerated]
    [global::System.Serializable]
    public new class UxmlSerializedData : UnityEngine.UIElements.VisualElement.UxmlSerializedData
    {
        #pragma warning disable 649
        [SerializeField] private Person manager;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags manager_UxmlAttributeFlags;
        [SerializeField] private System.Collections.Generic.List<Person> employees;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags employees_UxmlAttributeFlags;
        #pragma warning restore 649

        public override object CreateInstance() => new Department();
        public override void Deserialize(object obj)
        {
            base.Deserialize(obj);
            var e = (Department)obj;
            if (ShouldWriteAttributeValue(manager_UxmlAttributeFlags))
            {
                if (this.manager != null)
                {
                    e.manager = global::UnityEngine.UIElements.UxmlSerializedDataCreator.CopySerialized<Example.Person>(this.manager);
                }
            }
            if (ShouldWriteAttributeValue(employees_UxmlAttributeFlags))
            {
                if (this.employees != null)
                {
                    e.employees = global::UnityEngine.UIElements.UxmlSerializedDataCreator.CopySerialized<System.Collections.Generic.List<Example.P
                }
            }
        }
    }
}
```

It's crucial to remember that when generating code for **UxmlObjects**, the **UxmlSerializedData** differs from standard elements. In this context, the field for the **UxmlObject** references the **UxmlSerializedData**, not the actual **UxmlObject** type. This distinction is particularly significant when you're looking to create custom property drawers.

The following is the generated code for our previous **UxmlObject** example.
We now have 2 generated classes:

### Person

> Note: Unlike the **AttributeConverter** example, there's no need to mark the `Person` class as **Serializable** in this case. The serialization process is handled entirely through its **UxmlSerializedData** class.

```c#
public partial class Person
{
    [global::System.Runtime.CompilerServices.CompilerGenerated]
    [global::System.Serializable]
    public class UxmlSerializedData : global::UnityEngine.UIElements.UxmlSerializedData
    {
        #pragma warning disable 649
        [SerializeField] private string name;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags name_UxmlAttributeFlags;
        [SerializeField] private int age;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags age_UxmlAttributeFlags;
        [SerializeField] private string nationality;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags nationality_UxmlAttributeFlags;
        #pragma warning restore 649

        public override object CreateInstance() => new Person();
        public override void Deserialize(object obj)
        {
            var e = (Person)obj;
            if (ShouldWriteAttributeValue(name_UxmlAttributeFlags))
                e.name = this.name;
            if (ShouldWriteAttributeValue(age_UxmlAttributeFlags))
                e.age = this.age;
            if (ShouldWriteAttributeValue(nationality_UxmlAttributeFlags))
                e.nationality = this.nationality;
        }
    }
}
```

### Department

> Note: It's worth pointing out that the **UxmlObjectReference** field employs [SerializeReference](https://docs.unity3d.com/ScriptReference/SerializeReference.html). This feature enables us to handle derived types of the UxmlObject, offering greater flexibility and versatility.

```c#
public partial class Department
{
    [global::System.Runtime.CompilerServices.CompilerGenerated]
    [global::System.Serializable]
    public new class UxmlSerializedData : UnityEngine.UIElements.VisualElement.UxmlSerializedData
    {
        #pragma warning disable 649
        [UxmlObjectReferenceAttribute("manager")]
        [SerializeReference] private Example.Person.UxmlSerializedData manager;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags manager_UxmlAttributeFlags;
        [UnityEngine.UIElements.UxmlObjectReferenceAttribute("employees")]
        [SerializeReference] private System.Collections.Generic.List<Example.Person.UxmlSerializedData> employees;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags employees_UxmlAttributeFlags;
        #pragma warning restore 649
        
        public override object CreateInstance() => new Department();
        public override void Deserialize(object obj)
        {
            base.Deserialize(obj);
            var e = (Department)obj;

            if (ShouldWriteAttributeValue(manager_UxmlAttributeFlags))
            {
                if (this.manager != null)
                {
                    var managerInstance = (Example.Person)this.manager.CreateInstance();
                    this.manager.Deserialize(managerInstance);
                    e.manager = managerInstance;
                }
            }
            
            if (ShouldWriteAttributeValue(employees_UxmlAttributeFlags))
            {
                var employeesInstance = new global::System.Collections.Generic.List<Example.Person>();
                if (this.employees != null)
                {
                    for (int i = 0; i < this.employees.Count; ++i)
                    {
                        if (this.employees[i] != null)
                        {
                            var employeesItemInstance = (Example.Person)this.employees[i].CreateInstance();
                            this.employees[i].Deserialize(employeesItemInstance);
                            employeesInstance.Add(employeesItemInstance);
                        }
                        else
                        {
                            employeesInstance.Add(null);
                        }
                    }
                }
                e.employees = employeesInstance;
            }
        }
    }
}
```

## Custom Property Drawers

Now that we are using SerializedFields we can support Custom Property drawer. A custom property drawer can be applied to either the type or the field, just like a [ScriptableObject](https://docs.unity3d.com/ScriptReference/ScriptableObject.html) or [MonoBehaviour](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html).

Let's delve into a more advanced example: constructing an inventory system using UxmlObjects.

To start, we'll create an Item class. This class will be abstract and serve as a blueprint for all types of objects, encompassing their shared properties.

```c#
[UxmlObject]
public abstract partial class Item
{
    [UxmlAttribute, HideInInspector]
    public int id;

    [UxmlAttribute]
    public string name;

    [UxmlAttribute]
    public float weight;
}
```

Now, let's forge a variety of items. We'll craft a Health Pack along with different types of weapons.

```c#
[UxmlObject]
public partial class HealthPack : Item
{
    [UxmlAttribute]
    public float healAmount = 100;

    public HealthPack()
    {
        name = "Health Pack";
    }
}

[UxmlObject]
public partial class Sword : Item
{
    [UxmlAttribute, Range(1, 100)]
    public float slashDamage;
}

[Serializable]
public class Ammo
{
    public int count;
    public int maxCount;
}

[UxmlObject]
public partial class Gun : Item
{
    [UxmlAttribute]
    public float damage;

    [UxmlAttribute]
    public Ammo ammo = new Ammo {  count = 10, maxCount = 10 };
}
```

Since we're utilizing a custom attribute named `Ammo`, it's necessary to define an attribute converter for it as well.

```c#
public class AmmoConverter : UxmlAttributeConverter<Ammo>
{
    public override Ammo FromString(string value)
    {
        var ammo = new Ammo();
        var values = value.Split('/');
        if (values.Length == 2)
        {
            int.TryParse(values[0], out ammo.count);
            int.TryParse(values[1], out ammo.maxCount);
        }
        return ammo;
    }

    public override string ToString(Ammo value)
    {
        return $"{value.count}/{value.maxCount}";
    }
}
```

To store all of our items we will need an Inventory class:

```c#
[UxmlObject]
public partial class Inventory
{
    List<Item> m_Items = new List<Item>();
    Dictionary<int, Item> m_ItemDictionary = new Dictionary<int, Item>();

    [UxmlAttribute]
    int nextItemId = 1;

    [UxmlObjectReference("items")]
    public List<Item> items
    {
        get => m_Items;
        set
        {
            m_Items = value;
            m_ItemDictionary.Clear();
            foreach (var item in m_Items)
            {
                m_ItemDictionary[item.id] = item;
            }
        }
    }

    public Item GetItem(int id) => m_ItemDictionary.TryGetValue(id, out var item) ? item : null;
}
```

Finally we need to use the Inventory, we can do this inside of a **VisualElement** like so:

```c#
[UxmlElement]
public partial class Character : VisualElement
{
    [UxmlObjectReference("inventory")]
    public Inventory Inventory { get; set; } = new Inventory();
}
```

When you add the Character element to the UI Builder, you'll notice that you can add and remove inventory items. However, you might observe that the id value is not automatically assigned. Additionally, it would be a valuable enhancement to offer preconfigured inventories to assist users when configuring the `Character`.
We can add a **CustomProperty** drawer to improve this.

```c#
[CustomPropertyDrawer(typeof(Inventory.UxmlSerializedData))]
public class InventoryPropertyDrawer : PropertyDrawer
{
    SerializedProperty m_InventoryProperty;
    SerializedProperty m_ItemsProperty;

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        m_InventoryProperty = property;

        var root = new VisualElement();

        m_ItemsProperty = property.FindPropertyRelative("items");
        var items = new ListView
        {
            showAddRemoveFooter = true,
            showBorder = true,
            showFoldoutHeader = false,
            reorderable = true,
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            reorderMode = ListViewReorderMode.Animated,
            bindingPath = m_ItemsProperty.propertyPath,
            overridingAddButtonBehavior = OnAddItem
        };
        root.Add(items);

        var addSniperGear = new Button(() =>
        {
            AddGun("Rifle", 4.5f, 33, 30, 30);
            AddSword("Knife", 0.5f, 7);
            AddHealthPack();
            m_InventoryProperty.serializedObject.ApplyModifiedProperties();
        });
        addSniperGear.text = "Add Sniper Gear";

        var addWarriorGear = new Button(() =>
        {
            AddGun("Rifle", 4.5f, 33, 30, 30);
            AddHealthPack();
            AddSword("Machete", 1, 11);
            m_InventoryProperty.serializedObject.ApplyModifiedProperties();
        });
        addWarriorGear.text = "Add Warrior Gear";

        var addMedicGear = new Button(() =>
        {
            AddGun("Pistol", 1.5f, 10, 15, 15);
            AddHealthPack();
            AddHealthPack();
            AddHealthPack();
            m_InventoryProperty.serializedObject.ApplyModifiedProperties();
        });
        addMedicGear.text = "Add Medic Gear";

        root.Add(addSniperGear);
        root.Add(addWarriorGear);
        root.Add(addMedicGear);
        root.Bind(property.serializedObject);
        return root;
    }

    void AddGun(string name, float weight, float damage, int ammo, int maxAmmo)
    {
        m_ItemsProperty.arraySize++;
        var newItem = m_ItemsProperty.GetArrayElementAtIndex(m_ItemsProperty.arraySize - 1);
        newItem.managedReferenceValue = UxmlSerializedDataCreator.CreateUxmlSerializedData(typeof(Gun));
        newItem.FindPropertyRelative("id").intValue = NextItemId();
        newItem.FindPropertyRelative("name").stringValue = name;
        newItem.FindPropertyRelative("weight").floatValue = weight;
        newItem.FindPropertyRelative("damage").floatValue = damage;
        var ammoInstance = newItem.FindPropertyRelative("ammo");
        ammoInstance.FindPropertyRelative("count").intValue = ammo;
        ammoInstance.FindPropertyRelative("maxCount").intValue = maxAmmo;
    }

    void AddSword(string name, float weight, float damage)
    {
        m_ItemsProperty.arraySize++;
        var newItem = m_ItemsProperty.GetArrayElementAtIndex(m_ItemsProperty.arraySize - 1);
        newItem.managedReferenceValue = UxmlSerializedDataCreator.CreateUxmlSerializedData(typeof(Sword));
        newItem.FindPropertyRelative("id").intValue = NextItemId();
        newItem.FindPropertyRelative("name").stringValue = name;
        newItem.FindPropertyRelative("weight").floatValue = weight;
        newItem.FindPropertyRelative("slashDamage").floatValue = damage;
    }

    void AddHealthPack()
    {
        m_ItemsProperty.arraySize++;
        var newItem = m_ItemsProperty.GetArrayElementAtIndex(m_ItemsProperty.arraySize - 1);
        newItem.managedReferenceValue = UxmlSerializedDataCreator.CreateUxmlSerializedData(typeof(HealthPack));
        newItem.FindPropertyRelative("id").intValue = NextItemId();
    }

    int NextItemId() => m_InventoryProperty.FindPropertyRelative("nextItemId").intValue++;

    void OnAddItem(BaseListView baseListView, Button button)
    {
        var menu = new GenericMenu();
        var items = TypeCache.GetTypesDerivedFrom<Item>();
        foreach (var item in items)
        {
            if (item.IsAbstract)
                continue;

            menu.AddItem(new GUIContent(item.Name), false, () =>
            {
                m_ItemsProperty.arraySize++;
                var newItem = m_ItemsProperty.GetArrayElementAtIndex(m_ItemsProperty.arraySize - 1);
                newItem.managedReferenceValue = UxmlSerializedDataCreator.CreateUxmlSerializedData(item);
                newItem.FindPropertyRelative("id").intValue = NextItemId();
                m_InventoryProperty.serializedObject.ApplyModifiedProperties();
            });
        }

        menu.DropDown(button.worldBound);
    }
}
```

Let's dive deeper into the **PropertyDrawer**. The key point to highlight is that we're editing the **UxmlSerializedData**, not the `Inventory` directly.

```c#
[CustomPropertyDrawer(typeof(Inventory.UxmlSerializedData))]
```

When adding a new **UxmlObject** to the inventory list, it's essential to remember to include an instance of the UxmlSerializedData and not an `Item` instance. To simplify this process, we offer a convenient utility method called `UxmlSerializedDataCreator.CreateUxmlSerializedData`. This method not only generates an instance of the **UxmlObject's** **UxmlSerializedData** but also ensures that it comes with the appropriate default values pre-assigned.

```c#
newItem.managedReferenceValue = UxmlSerializedDataCreator.CreateUxmlSerializedData(typeof(HealthPack));
```

In our approach, we've introduced the assignment of an id value. To manage this, we store the last used id value within the element as a hidden field labeled `nextItemId`. Additionally, we've incorporated buttons that allow users to easily add preconfigured sets of items. For instance, a Soldier might receive a Rifle, Machete, and Health Pack.

Finally we can add a CustomProperty drawer for our `Ammo` class so that we can clamp the `count` value to be less than `maxCount`.

> Note: This time we are creating a **PropertyDrawer** for the `Ammo` type because its not a **UxmlObject**.

```c#
[CustomPropertyDrawer(typeof(Ammo))]
public class AmmoPropertyDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = new VisualElement { style = { flexDirection = FlexDirection.Row } };

        var count = property.FindPropertyRelative("count");
        var maxCount = property.FindPropertyRelative("maxCount");

        var ammoField = new IntegerField("Ammo") { isDelayed = true, bindingPath = count.propertyPath };
        ammoField.RegisterValueChangedCallback(e =>
        {
            count.intValue = Mathf.Min(maxCount.intValue, e.newValue);
            property.serializedObject.ApplyModifiedProperties();
        });
        root.Add(ammoField);
        root.Add(new Label("/"));

        var countField = new IntegerField { isDelayed = true, bindingPath = property.FindPropertyRelative("maxCount").propertyPath };
        countField.RegisterValueChangedCallback(e =>
        {
            count.intValue = Mathf.Min(e.newValue, count.intValue);
            property.serializedObject.ApplyModifiedProperties();
        });
        root.Add(countField);

        root.Bind(property.serializedObject);

        return root;
    }
}
```

Example UXML:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" editor-extension-mode="False">
    <Character name="Sniper">
        <inventory>
            <Inventory next-item-id="4">
                <items>
                    <Gun id="1" name="Rifle" weight="4.5" damage="33" ammo="7/7" />
                    <Sword id="2" name="Knife" weight="0.5" slash-damage="7" />
                    <HealthPack id="3" />
                </items>
            </Inventory>
        </inventory>
    </Character>
</ui:UXML>
```

## Overriding attributes

An additional noteworthy feature is what we refer to as "overriding attributes." This capability not only allows you to substitute the get and set behavior for a UXML attribute but also replaces the attribute itself with your overridden version in the UI Builder attributes view. This can be particularly handy for customizing attributes inherited from child classes. For instance, you might want to enforce value limits in an **IntegerField**, which can be achieved by overriding the value attribute and applying the [Range](https://docs.unity3d.com/ScriptReference/RangeAttribute.html) attribute like so:

```c#
[UxmlElement]
public partial class MyCustomIntField : IntegerField
{
    [UxmlAttribute("value"), Range(0, 100)]
    private int valueOverride
    {
        get => this.value;
        set => this.value = value;
    }
}
```

## Conclusion

We hope that this introduction has been helpful, if you have further questions you can ask them on discussions under the [UI Toolkit tag](https://discussions.unity.com/tag/ui-toolkit).

Be sure to also check out our scripting docs which include further information and examples for [UxmlElement](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/UIElements.UxmlElementAttribute.html) and [UxmlObjects](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/UIElements.UxmlObjectAttribute.html).

If you want to build your VisualElements into a library file then [here](https://discussions.unity.com/t/ui-toolkit-serialization-and-library-compilation-dll-support/1524794) is a guide to run the code generator during the build.

All the above examples can be found in this [project](https://github.com/karljj1/UxmlSerializedDataBlogPost).
