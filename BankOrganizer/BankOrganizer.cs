using MelonLoader;
using UnityEngine;
using BankOrganizer.UI;

namespace BankOrganizer;

public class BankOrganizer : MelonMod
{
    public const string ModVersion = "0.0.0";

    private BankOrganizerUI? _ui;

    public override void OnInitializeMelon()
    {
        _ui = new BankOrganizerUI();
        _ui.Initialize();
    }

    public override void OnGUI()
    {

    }

    public override void OnApplicationQuit()
    {

    }

    public override void OnUpdate()
    {
        _ui?.HandleInput();
    }

    public override void OnDeinitializeMelon()
    {
        _ui?.Cleanup();
    }
}