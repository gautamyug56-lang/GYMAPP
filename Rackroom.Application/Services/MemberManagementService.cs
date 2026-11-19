using System.Net.Mail;
using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;
using Rackroom.Domain.Entities;

namespace Rackroom.Application.Services;

public interface IMemberManagementService
{
    Task<Member> AddMemberAsync(
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default);

    Task<Member?> GetMemberAsync(
        Guid memberId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Member>> SearchMembersAsync(
        string? search,
        CancellationToken cancellationToken = default);

    Task EditMemberAsync(
        Guid memberId,
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default);

    Task DeactivateMemberAsync(
        Guid memberId,
        CancellationToken cancellationToken = default);
}

public sealed class MemberManagementService : IMemberManagementService
{
    private readonly ICurrentGymContext _currentGymContext;
    private readonly IRackroomDataStore _dataStore;

    public MemberManagementService(
        ICurrentGymContext currentGymContext,
        IRackroomDataStore dataStore)
    {
        _currentGymContext = currentGymContext;
        _dataStore = dataStore;
    }

    public async Task<Member> AddMemberAsync(
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default)
    {
        Guid gymId = GetGymId();

        string normalizedFirstName = NormalizeRequired(firstName, nameof(firstName));
        string normalizedLastName = NormalizeRequired(lastName, nameof(lastName));
        string normalizedEmail = NormalizeEmail(email);

        if (await _dataStore.MemberEmailExistsAsync(gymId, normalizedEmail, null, cancellationToken))
        {
            throw new BusinessRuleException("A member with this email already exists in this gym.");
        }

        Member member = new()
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            FirstName = normalizedFirstName,
            LastName = normalizedLastName,
            Email = normalizedEmail,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddMemberAsync(member, cancellationToken);
        await _dataStore.SaveChangesAsync(cancellationToken);

        return member;
    }

    public Task<Member?> GetMemberAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        return _dataStore.GetMemberAsync(GetGymId(), memberId, cancellationToken);
    }

    public Task<IReadOnlyList<Member>> SearchMembersAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        return _dataStore.SearchMembersAsync(GetGymId(), search, cancellationToken);
    }

    public async Task EditMemberAsync(
        Guid memberId,
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default)
    {
        Guid gymId = GetGymId();
        Member member = await RequireMemberAsync(gymId, memberId, cancellationToken);

        string normalizedEmail = NormalizeEmail(email);

        if (await _dataStore.MemberEmailExistsAsync(
                gymId,
                normalizedEmail,
                memberId,
                cancellationToken))
        {
            throw new BusinessRuleException("A member with this email already exists in this gym.");
        }

        member.FirstName = NormalizeRequired(firstName, nameof(firstName));
        member.LastName = NormalizeRequired(lastName, nameof(lastName));
        member.Email = normalizedEmail;

        await _dataStore.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateMemberAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        Member member = await RequireMemberAsync(GetGymId(), memberId, cancellationToken);

        if (!member.IsActive)
        {
            throw new BusinessRuleException("The member is already inactive.");
        }

        member.IsActive = false;
        await _dataStore.SaveChangesAsync(cancellationToken);
    }

    private async Task<Member> RequireMemberAsync(
        Guid gymId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        return await _dataStore.GetMemberAsync(gymId, memberId, cancellationToken)
            ?? throw new BusinessRuleException("Member was not found in the current gym.");
    }

    private Guid GetGymId()
    {
        return _currentGymContext.GymId
            ?? throw new BusinessRuleException("No gym is associated with the current user.");
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        string result = value.Trim();

        if (string.IsNullOrWhiteSpace(result))
        {
            throw new BusinessRuleException($"{parameterName} is required.");
        }

        return result;
    }

    private static string NormalizeEmail(string value)
    {
        string result = value.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(result))
        {
            throw new BusinessRuleException("Email is required.");
        }

        try
        {
            MailAddress address = new(result);

            if (!string.Equals(address.Address, result, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("Email is not valid.");
            }
        }
        catch (FormatException)
        {
            throw new BusinessRuleException("Email is not valid.");
        }

        return result;
    }
}
