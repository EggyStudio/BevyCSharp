namespace Bevy;

/// <summary>What a <see cref="MenuEvent"/> asks of a menu, as Bevy's <c>MenuAction</c>.</summary>
public enum MenuAction
{
    /// <summary>Open the menu if it is closed, and focus the item <see cref="MenuEvent.Navigation"/> says.</summary>
    Open,

    /// <summary>Open the menu if it is closed and close it if it is open, as its button asks.</summary>
    Toggle,

    /// <summary>Close every menu open, as a chosen item asks.</summary>
    CloseAll,

    /// <summary>Give the focus back to the menu's owner, as Escape asks.</summary>
    FocusRoot,
}
