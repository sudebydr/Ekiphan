using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Content;

internal sealed class ContactTaxonomyService(EkiphanDbContext dbContext)
    : IContactTaxonomyService
{
    public async Task<AdminContactTaxonomy> GetAdminAsync(
        CancellationToken cancellationToken = default) =>
        new(
            await dbContext.ContactReasons.AsNoTracking()
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Name)
                .Select(item => new AdminContactReason(
                    item.Id, item.Name, item.SortOrder, item.IsActive,
                    item.IsComplaintReason))
                .ToListAsync(cancellationToken),
            await dbContext.ComplaintCategories.AsNoTracking()
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Name)
                .Select(item => new AdminComplaintCategory(
                    item.Id, item.ContactReasonId, item.Name, item.SortOrder,
                    item.IsActive))
                .ToListAsync(cancellationToken));

    public async Task<PublicContactTaxonomy> GetPublicAsync(
        CancellationToken cancellationToken = default)
    {
        var reasons = await dbContext.ContactReasons.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .Select(item => new AdminContactReason(
                item.Id, item.Name, item.SortOrder, true,
                item.IsComplaintReason))
            .ToListAsync(cancellationToken);
        var reasonIds = reasons.Select(item => item.Id).ToArray();
        var categories = await dbContext.ComplaintCategories.AsNoTracking()
            .Where(item => item.IsActive && reasonIds.Contains(item.ContactReasonId))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .Select(item => new AdminComplaintCategory(
                item.Id, item.ContactReasonId, item.Name, item.SortOrder, true))
            .ToListAsync(cancellationToken);
        return new PublicContactTaxonomy(reasons, categories);
    }

    public async Task<AdminContactReason> CreateReasonAsync(
        SaveContactReasonCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var reason = new ContactReason(
            Guid.NewGuid(), command.Name, command.SortOrder,
            command.IsComplaintReason);
        reason.SetActive(command.IsActive);
        dbContext.ContactReasons.Add(reason);
        await SaveAsync(cancellationToken);
        return ToReason(reason);
    }

    public async Task<AdminContactReason?> UpdateReasonAsync(
        Guid id,
        SaveContactReasonCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var reason = await dbContext.ContactReasons.SingleOrDefaultAsync(
            item => item.Id == id, cancellationToken);
        if (reason is null) return null;
        if (reason.IsComplaintReason != command.IsComplaintReason)
        {
            throw new ArgumentException(
                "Whether a reason accepts complaint categories cannot be changed.");
        }

        reason.Update(command.Name, command.SortOrder);
        reason.SetActive(command.IsActive);
        await SaveAsync(cancellationToken);
        return ToReason(reason);
    }

    public async Task<AdminComplaintCategory> CreateComplaintCategoryAsync(
        SaveComplaintCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var category = new ComplaintCategory(
            Guid.NewGuid(), command.ContactReasonId, command.Name,
            command.SortOrder);
        category.SetActive(command.IsActive);
        dbContext.ComplaintCategories.Add(category);
        await SaveAsync(cancellationToken);
        return ToCategory(category);
    }

    public async Task<AdminComplaintCategory?> UpdateComplaintCategoryAsync(
        Guid id,
        SaveComplaintCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = await dbContext.ComplaintCategories.SingleOrDefaultAsync(
            item => item.Id == id, cancellationToken);
        if (category is null) return null;
        if (category.ContactReasonId != command.ContactReasonId)
        {
            throw new ArgumentException(
                "An existing complaint category cannot be moved to another reason.");
        }

        await ValidateAsync(command, cancellationToken);
        category.Update(command.Name, command.SortOrder);
        category.SetActive(command.IsActive);
        await SaveAsync(cancellationToken);
        return ToCategory(category);
    }

    private async Task ValidateAsync(
        SaveComplaintCategoryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.ContactReasonId == Guid.Empty || command.SortOrder < 0 ||
            string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > 150)
        {
            throw new ArgumentException("Complaint category values are invalid.");
        }

        var acceptsComplaints = await dbContext.ContactReasons.AnyAsync(
            item => item.Id == command.ContactReasonId && item.IsComplaintReason,
            cancellationToken);
        if (!acceptsComplaints)
        {
            throw new ArgumentException(
                "Complaint categories can only belong to the customer complaint reason.");
        }
    }

    private static void Validate(SaveContactReasonCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.SortOrder < 0 || string.IsNullOrWhiteSpace(command.Name) ||
            command.Name.Trim().Length > 150)
        {
            throw new ArgumentException("Contact reason values are invalid.");
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ContactTaxonomyConflictException(
                "The name is already in use or a complaint reason already exists.");
        }
    }

    public async Task<bool> DeleteReasonAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var reason = await dbContext.ContactReasons.SingleOrDefaultAsync(
            item => item.Id == id, cancellationToken);
        if (reason is null) return false;
        if (await dbContext.ComplaintCategories.AnyAsync(
            item => item.ContactReasonId == id, cancellationToken))
        {
            throw new ContactTaxonomyConflictException(
                "This reason still has complaint categories attached. " +
                "Delete those categories first.");
        }

        dbContext.ContactReasons.Remove(reason);
        await DeleteAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteComplaintCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await dbContext.ComplaintCategories.SingleOrDefaultAsync(
            item => item.Id == id, cancellationToken);
        if (category is null) return false;

        dbContext.ComplaintCategories.Remove(category);
        await DeleteAsync(cancellationToken);
        return true;
    }

    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ContactTaxonomyConflictException(
                "This item is used by existing contact requests and cannot " +
                "be deleted. Deactivate it instead.");
        }
    }

    private static AdminContactReason ToReason(ContactReason item) =>
        new(item.Id, item.Name, item.SortOrder, item.IsActive,
            item.IsComplaintReason);

    private static AdminComplaintCategory ToCategory(ComplaintCategory item) =>
        new(item.Id, item.ContactReasonId, item.Name, item.SortOrder,
            item.IsActive);
}