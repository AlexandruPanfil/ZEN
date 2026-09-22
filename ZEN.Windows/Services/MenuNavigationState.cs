using System.Collections.Generic;
using ZEN.Application.DTOs.CashDeskMenu;

namespace ZEN.Windows.Services;

public sealed class MenuNavigationState
{
    private readonly Stack<MenuNavigationContext> _navigationStack = new();

    public MenuNavigationContext? CurrentContext => _navigationStack.Count > 0 ? _navigationStack.Peek() : null;

    public bool IsRoot => CurrentContext?.Type == MenuNavigationType.Root;

    public bool IsInGroup => CurrentContext?.Type == MenuNavigationType.MenuGroup || CurrentContext?.Type == MenuNavigationType.GroupReference;

    public event Action? OnStateChanged;

    public void NavigateToRoot()
    {
        _navigationStack.Clear();
        _navigationStack.Push(new MenuNavigationContext(MenuNavigationType.Root, null, null, null));
        OnStateChanged?.Invoke();
    }

    public void NavigateToGroup(Guid groupId, bool isSharedGroup, Guid? parentId = null, MenuNavigationType? parentType = null)
    {
        var type = isSharedGroup ? MenuNavigationType.GroupReference : MenuNavigationType.MenuGroup;
        _navigationStack.Push(new MenuNavigationContext(type, groupId, parentId, parentType));
        OnStateChanged?.Invoke();
    }

    public void NavigateBack()
    {
        if (_navigationStack.Count > 1)
        {
            _navigationStack.Pop();
            OnStateChanged?.Invoke();
        }
    }

    public void Clear()
    {
        _navigationStack.Clear();
        OnStateChanged?.Invoke();
    }

    public MenuNavigationContext? GetParentContext()
    {
        if (_navigationStack.Count > 1)
        {
            var temp = _navigationStack.Pop();
            var parent = _navigationStack.Peek();
            _navigationStack.Push(temp);
            return parent;
        }
        return null;
    }
}

public record MenuNavigationContext(
    MenuNavigationType Type,
    Guid? Id,
    Guid? ParentId,
    MenuNavigationType? ParentType);

public enum MenuNavigationType
{
    Root,
    MenuGroup,
    GroupReference
}