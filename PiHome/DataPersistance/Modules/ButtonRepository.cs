using DataPersistance.Models;
using Microsoft.EntityFrameworkCore;

namespace DataPersistance.Modules;

public class ButtonRepository
{
    private readonly PiHomeContext context;

    public ButtonRepository(PiHomeContext context)
    {
        this.context = context;
    }

    public Button[] GetAllButtons()
    {
        return context.Buttons.AsNoTracking().ToArray();
    }

    public ButtonMapping[] GetAllActions()
    {
        return context.ButtonMappings.AsNoTracking().ToArray();
    }

    public void UpdateButton(int id, string newName, int newToggleGroup)
    {
        var button = context.Buttons.Single(x => x.Id == id);
        button.Name = newName;
        button.ToggleGroup = newToggleGroup;
        context.SaveChanges();
    }

    public void AddButton(string name, int toggleGroup)
    {
        context.Buttons.Add(new Button { Name = name, ToggleGroup = toggleGroup });
        context.SaveChanges();
    }

    public void DeleteButton(int id)
    {
        var button = context.Buttons.Single(x => x.Id == id);
        context.Buttons.Remove(button);
        context.SaveChanges();
    }

    public void AddAction(int buttonId, int actionId, int onPresetId, int offPresetId, string description)
    {
        context.ButtonMappings.Add(new ButtonMapping
        {
            ActionId = actionId,
            ButtonId = buttonId,
            Description = description,
            ToggleOnPresetId = onPresetId,
            ToggleOffPresetId = offPresetId
        });
        context.SaveChanges();
    }

    public void EditAction(int buttonId, int actionId, int onPresetId, int offPresetId, string description)
    {
        var mapping = context.ButtonMappings.Single(x => x.ActionId == actionId && x.ButtonId == buttonId);
        mapping.ToggleOnPresetId = onPresetId;
        mapping.ToggleOffPresetId = offPresetId;
        mapping.Description = description;
        context.SaveChanges();
    }

    public void DeleteAction(int buttonId, int actionId)
    {
        var mapping = context.ButtonMappings.Single(x => x.ActionId == actionId && x.ButtonId == buttonId);
        context.ButtonMappings.Remove(mapping);
        context.SaveChanges();
    }

    public string? ButtonToggled(int buttonId, int actionId)
    {
        string? presetName;
        var mapping = context.ButtonMappings
            .Include(x => x.ToggleOnPreset)
            .Include(x => x.ToggleOffPreset)
            .Include(x => x.Button)
            .Single(x => x.ActionId == actionId && x.ButtonId == buttonId);
        var toggledNew = !mapping.Button.Toggled;
        presetName = toggledNew ? mapping.ToggleOnPreset?.Name : mapping.ToggleOffPreset?.Name;

        var groupButtons = context.Buttons
            .Where(x => x.ToggleGroup == mapping.Button.ToggleGroup)
            .AsEnumerable();
        foreach (var button in groupButtons)
        {
            button.Toggled = toggledNew;
        }
        mapping.Button.LastActivation = DateTime.Now;
        context.SaveChanges();

        return presetName;
    }
}