using UnityEngine.UIElements;

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
