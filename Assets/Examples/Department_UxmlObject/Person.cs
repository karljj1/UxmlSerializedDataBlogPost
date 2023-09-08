using System;
using UnityEngine.UIElements;

namespace UxmlObjectExample
{
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
}