namespace Vdlcrm.Model;

public class EndpointPermission
{
    public int Id { get; set; }
    public string RouteUrl { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? CreatedBy { get; set; }
}