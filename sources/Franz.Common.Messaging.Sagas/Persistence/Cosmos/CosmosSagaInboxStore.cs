#nullable enable

using System.Text.Json;
using Franz.Common.Headers;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Context;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Storage;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.Cosmos;

public sealed class CosmosSagaInboxStore : IInboxStore
{
  private readonly CosmosSagaDbContext _db;
  private readonly CosmosSagaUnitOfWork _unitOfWork;

  public CosmosSagaInboxStore(CosmosSagaDbContext dbContext, CosmosSagaUnitOfWork unitOfWork)
  {
    _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
  }

  public async Task<bool> HasProcessedAsync(Guid messageId, CancellationToken ct = default)
  {
    Validate(messageId);
    var tenantId = CurrentTenantId();
    return await FindAsync(messageId, tenantId, ct).ConfigureAwait(false) is not null;
  }

  public async Task<bool> TryRegisterAsync(Guid messageId, CancellationToken ct = default)
  {
    Validate(messageId);
    var tenantId = CurrentTenantId();

    if (await FindAsync(messageId, tenantId, ct).ConfigureAwait(false) is not null)
      return false;

    await _db.Set<CosmosSagaInboxDocument>().AddAsync(new CosmosSagaInboxDocument
    {
      Id = messageId.ToString("N"),
      TenantId = tenantId,
      ProcessedOn = DateTime.UtcNow
    }, ct).ConfigureAwait(false);

    if (!_unitOfWork.IsActive)
      await _db.SaveChangesAsync(ct).ConfigureAwait(false);

    return true;
  }

  public async Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default)
      => _ = await TryRegisterAsync(messageId, ct).ConfigureAwait(false);

  private Task<CosmosSagaInboxDocument?> FindAsync(
      Guid messageId,
      string tenantId,
      CancellationToken ct)
      => _db.Set<CosmosSagaInboxDocument>()
          .FindAsync([messageId.ToString("N"), tenantId], ct).AsTask();

  private static string CurrentTenantId()
      => MediatorContext.TenantId ?? string.Empty;

  private static void Validate(Guid messageId)
  {
    if (messageId == Guid.Empty)
      throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
  }
}
