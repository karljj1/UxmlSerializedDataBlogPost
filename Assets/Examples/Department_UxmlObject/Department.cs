using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UxmlObjectExample
{
    [UxmlElement]
    public partial class Department : VisualElement
    {
        [UxmlObjectReference("manager")]
        public Person manager;

        [UxmlObjectReference("employees")]
        public List<Person> employees;
    }
}