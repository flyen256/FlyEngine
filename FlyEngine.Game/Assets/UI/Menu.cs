using System.Numerics;
using FlyEngine.Core.Engine;
using FlyEngine.Core.Engine.Gui;
using FlyEngine.Core.Engine.Gui.Elements;
using FlyEngine.Core.Engine.Gui.Layout;
using FlyEngine.Network;
using FlyEngine.Network.Serializable;
using ImGuiNET;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FlyEngine.Game.UI;

public class Menu : GuiWindow
{
    private readonly ILogger _logger = new Logger<Menu>(LoggerFactory.Create(builder => builder.AddConsole()));
    private readonly GuiContainer _menuElement;
    private readonly GuiContainer _playersListElement;

    protected override string Name => "Menu";

    private bool _menuLoading;

    public Menu()
    {
        Anchor = GuiAnchor.Center;
        _menuElement = new GuiContainer();

        var hostButton = new Button("Host", OnHostButtonClicked);
        var joinButton = new Button("Join", OnJoinButtonClicked);

        _menuElement.Children.Add(hostButton);
        _menuElement.Children.Add(joinButton);
        Element = _menuElement;

        _playersListElement = new GuiContainer();
        var playersList = new GuiList<PlayerData>(
            () => NetworkManager.Instance != null ? NetworkManager.Instance.PlayersData : [],
            peer =>
            {
                var container = new GuiContainer(Orientation.Horizontal);
                container.Children.Add(new Label($"({peer.Id.ToString()}) peer id: {peer.PeerId}, is host: {peer.IsHost}"));
                return container;
            });
        _playersListElement.Children.Add(playersList);
        _playersListElement.Children.Add(new Button("Disconnect", OnDisconnectButtonClicked));
    }

    protected override void OnEnable()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.Client.OnPeerDisconnectedEvent += OnClientPeerDisconnected;
    }

    public override void OnDisable()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.Client.OnPeerDisconnectedEvent -= OnClientPeerDisconnected;
    }

    private void OnClientPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Element = _menuElement;
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
        if (NetworkManager.Instance == null) return;
        button.Enabled = false;
        if (!NetworkManager.Instance.Server.Start(true))
        {
            button.Enabled = true;
            return;
        }
        button.Enabled = true;
        _playersListElement.Children.Add(new Button("Start Game", OnStartGameButtonClicked));
        Anchor = GuiAnchor.TopCenter;
        Element = _playersListElement;
    }

    private void OnStartGameButtonClicked(Button button)
    {
        
    }

    private async void OnJoinButtonClicked(Button button)
    {
        try
        {
            if (NetworkManager.Instance == null) return;
            button.Enabled = false;
            var start = await NetworkManager.Instance.Client.StartAsync();
            button.Enabled = true;
            if (!start) return;
            Anchor = GuiAnchor.TopCenter;
            Element = _playersListElement;
        }
        catch (Exception e)
        {
            Console.WriteLine($"OnJoinButtonClicked error: {e.Message}");
        }
    }

    private void OnDisconnectButtonClicked(Button button)
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.Shutdown();
        Anchor = GuiAnchor.Center;
        Element = _menuElement;
    }
}