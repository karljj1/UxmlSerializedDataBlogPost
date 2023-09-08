using UnityEngine;
using UnityEngine.UIElements;

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