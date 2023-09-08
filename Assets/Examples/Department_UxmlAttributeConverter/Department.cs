using System.Collections.Generic;
using UnityEngine.UIElements;

namespace AttributeConverterExample
{
    [UxmlElement]
    public partial class Department : VisualElement
    {
        [UxmlAttribute]
        public Person manager;

        [UxmlAttribute]
        public List<Person> employees;
    }
}