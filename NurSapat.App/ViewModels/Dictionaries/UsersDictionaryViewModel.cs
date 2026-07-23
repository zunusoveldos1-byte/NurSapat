using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Users;
using NurSapat.App.Services;
using NurSapat.App.Services.Users;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class UsersDictionaryViewModel : BaseDictionaryViewModel<UserDictionaryItemViewModel>
{
    private readonly IUsersStore _store;
    private readonly IUserEditorService _editorService;
    private readonly IUserSessionService _sessionService;
    private readonly IAppMessageService _messageService;

    public UsersDictionaryViewModel(
        IUsersStore store,
        IUserEditorService editorService,
        IUserSessionService sessionService,
        IAppMessageService messageService)
    {
        _store = store;
        _editorService = editorService;
        _sessionService = sessionService;
        _messageService = messageService;
    }

    public override string Title => "Справочник пользователей";

    public bool CanManageUsers => _sessionService.IsAdministrator;

    public string AccessNotice => CanManageUsers
        ? "Редактирование доступно: администраторские права подтверждены."
        : "Справочник доступен только для просмотра. Изменения требуют прав администратора.";

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    public IAsyncRelayCommand AddUserCommand => AddCommand;

    public IAsyncRelayCommand EditUserCommand => EditCommand;

    public IAsyncRelayCommand DeleteUserCommand => DeleteCommand;

    protected override bool MatchesSearch(UserDictionaryItemViewModel item, string search) =>
        Contains(item.Username, search)
        || item.GroupNumber.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);

    protected override void ApplyFilter()
    {
        var search = SearchText.Trim();
        IEnumerable<UserDictionaryItemViewModel> query = AllItems;

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item => MatchesSearch(item, search));
        }

        query = query.OrderBy(item => item.Username);

        FilteredItems.Clear();
        foreach (var item in query)
        {
            FilteredItems.Add(item);
        }

        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }

    protected override Task LoadItemsAsync()
    {
        AllItems.Clear();
        foreach (var user in _store.GetAll())
        {
            AllItems.Add(UserDictionaryItemViewModel.FromModel(user));
        }

        return Task.CompletedTask;
    }

    protected override async Task AddItemAsync()
    {
        if (!CanManageUsers)
        {
            _messageService.LogWarning("Добавление пользователей доступно только администратору.");
            return;
        }

        var draft = new UserAccountItem
        {
            Username = "Новый пользователь",
            GroupNumber = 1,
            Permissions = UserPermissions.CreateOperator()
        };

        var saved = await _editorService.ShowEditorAsync(OwnerWindow, draft, isNew: true);
        if (saved is null)
        {
            return;
        }

        if (_store.GetByUsername(saved.Username) is not null)
        {
            _messageService.LogError("Пользователь с таким именем уже существует.");
            return;
        }

        _store.Add(saved);
        var item = UserDictionaryItemViewModel.FromModel(saved);
        AllItems.Add(item);
        SelectedItem = item;
        _messageService.LogInfo($"Пользователь «{saved.Username}» добавлен.");
    }

    protected override async Task EditItemAsync(UserDictionaryItemViewModel item)
    {
        if (!CanManageUsers)
        {
            _messageService.LogWarning("Редактирование пользователей доступно только администратору.");
            return;
        }

        var source = _store.GetById(item.Id)?.Clone() ?? new UserAccountItem { Username = item.Username };
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, source, isNew: false);
        if (saved is null)
        {
            return;
        }

        var duplicate = _store.GetByUsername(saved.Username);
        if (duplicate is not null && duplicate.Id != saved.Id)
        {
            _messageService.LogError("Пользователь с таким именем уже существует.");
            return;
        }

        _store.Update(saved);
        item.RefreshFrom(saved);

        if (_sessionService.CurrentUser?.Id == saved.Id)
        {
            _sessionService.SetCurrentUser(saved);
        }

        _messageService.LogInfo($"Пользователь «{saved.Username}» сохранён.");
    }

    protected override Task DeleteItemAsync(UserDictionaryItemViewModel item)
    {
        if (!CanManageUsers)
        {
            _messageService.LogWarning("Удаление пользователей доступно только администратору.");
            return Task.CompletedTask;
        }

        if (_sessionService.CurrentUser?.Id == item.Id)
        {
            _messageService.LogError("Нельзя удалить текущего пользователя.");
            return Task.CompletedTask;
        }

        _store.Delete(item.Id);
        _messageService.LogInfo($"Пользователь «{item.Username}» удалён.");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    public async Task OpenEditorForItemAsync(UserDictionaryItemViewModel item)
    {
        SelectedItem = item;
        await EditCommand.ExecuteAsync(null);
    }
}
