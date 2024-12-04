using System.Collections.Generic;

namespace PiUi.Models;

public class ButtonOverviewViewModel
{
    public ButtonViewModel[] Buttons { get; set; }
    public Dictionary<int, string> Presets { get; set; }
    public ButtonActionViewModel[] Actions { get; set; }
}

public class ButtonViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int ToggleGroup { get; set; }
    public bool IsOn { get; set; }
}

public class ButtonActionViewModel
{
    public int ButtonId { get; set; }
    public int Action { get; set; }
    public int? OnPresetId { get; set; }
    public int? OffPresetId { get; set; }
    public string Description { get; set; }
}