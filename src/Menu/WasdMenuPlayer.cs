using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace TeleZone;

public class WasdMenuPlayer
{
    public CCSPlayerController Player { get; set; } = null!;
    public WasdMenu? MainMenu { get; private set; }
    public LinkedListNode<WasdMenuOption>? CurrentChoice { get; private set; }
    public LinkedListNode<WasdMenuOption>? MenuStart { get; private set; }
    public string CenterHtml { get; private set; } = "";
    public PlayerButtons Buttons { get; set; }

    private const int VisibleOptions = 5;

    public bool HasActiveMenu => MainMenu != null && CurrentChoice != null;

    public void OpenMenu(WasdMenu? menu)
    {
        if (menu == null)
        {
            CurrentChoice = null;
            CenterHtml = "";
            MainMenu = null;
            Player.PrintToCenterHtml(" ");
            return;
        }

        MainMenu = menu;
        CurrentChoice = menu.Options.First;
        MenuStart = CurrentChoice;
        UpdateHtml();
    }

    public void ScrollUp()
    {
        if (CurrentChoice == null || MainMenu == null) return;
        CurrentChoice = CurrentChoice.Previous ?? CurrentChoice.List?.Last;
        AdjustMenuStart();
        UpdateHtml();
    }

    public void ScrollDown()
    {
        if (CurrentChoice == null || MainMenu == null) return;
        CurrentChoice = CurrentChoice.Next ?? CurrentChoice.List?.First;
        AdjustMenuStart();
        UpdateHtml();
    }

    public void Choose()
    {
        if (CurrentChoice == null) return;
        Player.ExecuteClientCommand("play Ui/buttonrollover.vsnd_c");
        CurrentChoice.Value.OnChoose?.Invoke(Player, CurrentChoice.Value);
    }

    public void Close()
    {
        OpenMenu(null);
    }

    private void AdjustMenuStart()
    {
        if (CurrentChoice == null) return;
        int idx = CurrentChoice.Value.Index;

        var node = CurrentChoice.List?.First;
        int start = Math.Max(0, idx - VisibleOptions + 1);
        for (int i = 0; i < start && node != null; i++)
            node = node.Next;
        MenuStart = node ?? CurrentChoice.List?.First;
    }

    public void UpdateHtml()
    {
        if (CurrentChoice == null || MainMenu == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("<div>");

        if (!string.IsNullOrEmpty(MainMenu.Title))
            sb.AppendLine($"<b><font color='#aa44ff' class='fontSize-m'>{MainMenu.Title}</font></b><br>");

        var node = MenuStart;
        int shown = 0;
        while (shown < VisibleOptions && node != null)
        {
            if (node == CurrentChoice)
                sb.AppendLine($"<font color='yellow'>►[</font><font color='#9acd32' class='fontSize-m'>{node.Value.OptionDisplay}</font><font color='yellow'>]◄</font><br>");
            else
                sb.AppendLine($"<font color='white' class='fontSize-m'>{node.Value.OptionDisplay}</font><br>");

            shown++;
            node = node.Next;
        }

        if (node != null)
            sb.AppendLine("<font color='gray'>▼ more ▼</font><br>");

        sb.AppendLine("<font color='#ff3333' class='fontSize-sm'>Move: <font color='#f5a142'>[W/S]</font>  Select: <font color='#f5a142'>[E]</font>  Close: <font color='#f5a142'>[R]</font></font><br>");
        sb.AppendLine("</div>");

        CenterHtml = sb.ToString();
    }
}
