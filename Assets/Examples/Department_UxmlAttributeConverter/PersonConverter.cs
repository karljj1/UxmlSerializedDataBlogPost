#if UNITY_EDITOR

using UnityEditor.UIElements;

namespace AttributeConverterExample
{
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
}
#endif