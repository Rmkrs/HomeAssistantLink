namespace HomeAssistantLink.Host.TrayApp;

using HomeAssistantLink.UserSession.Contracts;

using Microsoft.Extensions.Hosting;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly IServiceSessionClient serviceSessionClient;
    private readonly IHostApplicationLifetime lifetime;
    private readonly NotifyIcon notifyIcon;
    private readonly ContextMenuStrip contextMenu;

    public TrayApplicationContext(
        IServiceSessionClient serviceSessionClient,
        IHostApplicationLifetime lifetime)
    {
        this.serviceSessionClient = serviceSessionClient;
        this.lifetime = lifetime;
        this.contextMenu = new ContextMenuStrip();

        this.contextMenu.Opening += this.ContextMenuOpening;

        this.notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "HomeAssistantLink User Session",
            Visible = true,
            ContextMenuStrip = this.contextMenu,
        };

        this.RebuildContextMenu();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.contextMenu.Opening -= this.ContextMenuOpening;
            this.notifyIcon.Visible = false;
            this.notifyIcon.Dispose();
            this.contextMenu.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        this.RebuildContextMenu();
    }

    private void RebuildContextMenu()
    {
        this.contextMenu.Items.Clear();

        this.contextMenu.Items.Add(
            text: "HomeAssistantLink User Session",
            image: null,
            onClick: (_, _) => { }).Enabled = false;

        var commandResult = this.serviceSessionClient.GetUserCommands();

        this.contextMenu.Items.Add(new ToolStripSeparator());

        if (!commandResult.Success)
        {
            this.contextMenu.Items.Add(
                text: $"Service unavailable: {commandResult.Error}",
                image: null,
                onClick: (_, _) => { }).Enabled = false;
        }
        else if (commandResult.Commands.Count > 0)
        {
            foreach (var command in commandResult.Commands)
            {
                var menuCommand = command;

                this.contextMenu.Items.Add(
                    text: menuCommand.DisplayName,
                    image: null,
                    onClick: (_, _) => this.serviceSessionClient.Execute(menuCommand.CommandId));
            }
        }
        else
        {
            this.contextMenu.Items.Add(
                text: "No user commands configured",
                image: null,
                onClick: (_, _) => { }).Enabled = false;
        }

        this.contextMenu.Items.Add(new ToolStripSeparator());

        this.contextMenu.Items.Add(
            text: "Exit",
            image: null,
            onClick: (_, _) => this.Exit());
    }

    private void Exit()
    {
        this.notifyIcon.Visible = false;
        this.lifetime.StopApplication();
        this.ExitThread();
    }
}
