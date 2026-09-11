namespace FplBot.WebApi.Endpoints.Api.Admin;

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
