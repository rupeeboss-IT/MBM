using Microsoft.EntityFrameworkCore;
using RB_Website_API.Models;

namespace RB_Website_API.Auth;

public static class AdminUserSort
{
    public static IQueryable<User> Apply(IQueryable<User> query, string sortField, bool sortAsc)
    {
        var field = (sortField ?? "").Trim().ToLowerInvariant();

        return field switch
        {
            "name" or "fullname" or "member" =>
                sortAsc ? query.OrderBy(u => u.FullName) : query.OrderByDescending(u => u.FullName),
            "email" =>
                sortAsc ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
            "phone" =>
                sortAsc ? query.OrderBy(u => u.Phone) : query.OrderByDescending(u => u.Phone),
            "role" =>
                sortAsc ? query.OrderBy(u => u.Role) : query.OrderByDescending(u => u.Role),
            "status" or "isactive" =>
                sortAsc ? query.OrderBy(u => u.IsActive) : query.OrderByDescending(u => u.IsActive),
            "memberid" =>
                sortAsc ? query.OrderBy(u => u.MemberId) : query.OrderByDescending(u => u.MemberId),
            _ =>
                sortAsc ? query.OrderBy(u => u.CreatedAt) : query.OrderByDescending(u => u.CreatedAt),
        };
    }
}
