using Microsoft.AspNetCore.Components;

namespace GridLike.Components;

public class RedirectToLogin : ComponentBase
{
    [Inject]
    protected NavigationManager? NavigationManager { get; set; }

    [Inject]
    protected IHttpContextAccessor? Context { get; set; }

    protected override void OnAfterRender(bool firstRender)
    {
        if (this.Context?.HttpContext?.User.Identity?.IsAuthenticated != true)
        {
            this.NavigationManager?.NavigateTo("/login", true);
        }
    }
}