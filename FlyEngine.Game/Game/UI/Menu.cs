using System.Numerics;
using FlyEngine.Core.Engine;
using FlyEngine.Core.Engine.UI;
using FlyEngine.Core.Engine.UI.Elements;
using FlyEngine.Core.Engine.UI.Layout;
using FlyEngine.Network;
using FlyEngine.Network.Serializable;
using ImGuiNET;
using LiteNetLib;

namespace FlyEngine.Game.UI;

public class Menu : UiRenderer
{
    private readonly UiContainer _menuElement;
    private readonly UiContainer _playersListElement;

    protected override string Name => "Menu";

    protected override Vector2 Position
    {
        get
        {
            var io = ImGui.GetIO();
            var offset = Vector2.Zero;
            foreach (var child in Element.Children)
            {
                offset.X -= child.Size.X / 4f;
                offset.Y -= child.Size.Y / 4f;
            }
            return new Vector2(io.DisplaySize.X / 2f + offset.X, io.DisplaySize.Y / 2f + offset.Y);
        }
    }

    private bool _menuLoading;

    public Menu()
    {
        _menuElement = new UiContainer();

        var hostButton = new Button("Host", OnHostButtonClicked);
        var joinButton = new Button("Join", OnJoinButtonClicked);

        _menuElement.Children.Add(hostButton);
        _menuElement.Children.Add(joinButton);
        Element = _menuElement;

        _playersListElement = new UiContainer();
        var playersList = new UiList<PlayerData>(
            () => NetworkManager.Instance != null ? NetworkManager.Instance.PlayersData : [],
            peer =>
            {
                var container = new UiContainer(Orientation.Horizontal);
                container.Children.Add(new Label($"({peer.Id.ToString()}) peer id: {peer.PeerId}, is host: {peer.IsHost}"));
                return container;
            });
        _playersListElement.Children.Add(playersList);
        _playersListElement.Children.Add(new Button("Disconnect", OnDisconnectButtonClicked));
    }
    
    public override void OnLoadUi()
    {
        var style = ImGui.GetStyle();
        style.WindowRounding = 10f;
        style.FrameRounding = 5f;
        style.FramePadding = new Vector2(5f, 5f);
        DebugTask().Start();
    }
    
    private Task DebugTask()
    {
        return new Task(() =>
        {
            while (Application.IsRunning)
            {
                
                Task.Delay(1000).Wait();
            }
        });
    }

    private void OnHostButtonClicked(Button button)
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.Initialized) return;
        button.Enabled = false;
        if (!NetworkManager.Instance.Server.Start(true))
        {
            button.Enabled = true;
            return;
        }

        button.Enabled = true;
        Element = _playersListElement;
    }

    private async void OnJoinButtonClicked(Button button)
    {
        try
        {
            if (NetworkManager.Instance == null ||
                !NetworkManager.Instance.Initialized) return;
            button.Enabled = false;
            var start = await NetworkManager.Instance.Client.StartAsync();
            button.Enabled = true;
            if (!start) return;
            Element = _playersListElement;
        }
        catch (Exception e)
        {
            Console.WriteLine($"OnJoinButtonClicked error: {e.Message}");
        }
    }

    private void OnDisconnectButtonClicked(Button button)
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.Initialized) return;
        NetworkManager.Instance.Shutdown();
        Element = _menuElement;
    }
}