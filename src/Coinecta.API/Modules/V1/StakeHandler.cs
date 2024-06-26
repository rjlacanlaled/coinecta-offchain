using System.Text;
using Cardano.Sync.Data.Models.Datums;
using CardanoSharp.Wallet.Utilities;
using Coinecta.Data;
using Coinecta.Data.Models;
using Coinecta.Data.Models.Api;
using Coinecta.Data.Models.Reducers;
using Coinecta.Data.Models.Response;
using Coinecta.Data.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coinecta.API.Modules.V1;

public class StakeHandler(
    IDbContextFactory<CoinectaDbContext> dbContextFactory,
    IConfiguration configuration,
    ILogger<TransactionHandler> logger
)
{
    public async Task<IResult> GetStakePoolAsync(string address, string ownerPkh, string policyId, string assetName)
    {
        try
        {
            using CoinectaDbContext dbContext = dbContextFactory.CreateDbContext();
            List<StakePoolByAddress> stakePools = await dbContext.StakePoolByAddresses.Where(s => s.Address == address).OrderByDescending(s => s.Slot).ToListAsync();

            return Results.Ok(
                stakePools
                    .Where(sp => Convert.ToHexString(sp.StakePool.Owner.KeyHash).Equals(ownerPkh, StringComparison.InvariantCultureIgnoreCase))
                    .Where(sp => sp.Amount.MultiAsset.ContainsKey(policyId.ToLowerInvariant()) && sp.Amount.MultiAsset[policyId].ContainsKey(assetName.ToLowerInvariant()))
                    .GroupBy(u => new { u.TxHash, u.TxIndex }) // Group by both TxHash and TxIndex
                    .Where(g => g.Count() < 2)
                    .Select(g => g.First())
                    .First()
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting stake pool");
            return Results.BadRequest(ex.Message);
        }
    }

    public async Task<IResult> GetStakePoolsAsync(string address, string ownerPkh)
    {
        using CoinectaDbContext dbContext = dbContextFactory.CreateDbContext();
        List<StakePoolByAddress> stakePools = await dbContext.StakePoolByAddresses.Where(s => s.Address == address).OrderByDescending(s => s.Slot).ToListAsync();

        return Results.Ok(
            stakePools
                .Where(sp => Convert.ToHexString(sp.StakePool.Owner.KeyHash).Equals(ownerPkh, StringComparison.InvariantCultureIgnoreCase))
                .GroupBy(u => new { u.TxHash, u.TxIndex }) // Group by both TxHash and TxIndex
                .Where(g => g.Count() < 2)
                .Select(g => g.First())
                .ToList()
        );
    }

    public async Task<IResult> GetStakeSummaryByStakeKeysAsync(List<string> stakeKeys)
    {
        if (stakeKeys.Count == 0) return Results.BadRequest("No stake keys provided");

        using CoinectaDbContext dbContext = dbContextFactory.CreateDbContext();

        // Current Timestamp
        DateTimeOffset dto = new(DateTime.UtcNow);
        ulong currentTimestamp = (ulong)dto.ToUnixTimeMilliseconds();

        // Get Stake Positions
        List<StakePositionByStakeKey> stakePositions = await dbContext.StakePositionByStakeKeys.Where(s => stakeKeys.Contains(s.StakeKey)).ToListAsync();

        // Filter Stake Positions
        IEnumerable<StakePositionByStakeKey> lockedPositions = stakePositions.Where(sp => sp.LockTime > currentTimestamp);
        IEnumerable<StakePositionByStakeKey> unclaimedPositions = stakePositions.Where(sp => sp.LockTime <= currentTimestamp);

        // Transaform Stake Positions
        StakeSummaryResponse result = new();

        stakePositions.ForEach(sp =>
        {
            // Remove NFT
            sp.Amount.MultiAsset.Remove(configuration["CoinectaStakeMintingPolicyId"]!);
            bool isLocked = sp.LockTime > currentTimestamp;
            string? policyId = sp.Amount.MultiAsset.Keys.FirstOrDefault();
            Dictionary<string, ulong> asset = sp.Amount.MultiAsset[policyId!];
            string assetName = asset.Keys.FirstOrDefault()!;
            string subject = policyId + assetName;
            ulong total = asset.Values.FirstOrDefault();

            if (result.PoolStats.TryGetValue(subject, out StakeStats? value))
            {
                value.TotalStaked += total;
                value.TotalPortfolio += total;
                value.UnclaimedTokens += isLocked ? 0 : total;
            }
            else
            {
                result.PoolStats[subject] = new StakeStats
                {
                    TotalStaked = total,
                    TotalPortfolio = total,
                    UnclaimedTokens = isLocked ? 0 : total
                };
            }

            result.TotalStats.TotalStaked += total;
            result.TotalStats.TotalPortfolio += total;
            result.TotalStats.UnclaimedTokens += isLocked ? 0 : total;
        });

        return Results.Ok(result);
    }

    public async Task<IResult> GetStakeRequestsByAddressesAsync(List<string> addresses, [FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        using CoinectaDbContext dbContext = dbContextFactory.CreateDbContext();

        int skip = (page - 1) * limit;

        List<StakeRequestByAddress> pagedData = await dbContext.StakeRequestByAddresses
            .Where(s => addresses.Contains(s.Address))
            .OrderByDescending(s => s.Slot)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();

        int totalCount = await dbContext.StakeRequestByAddresses
            .CountAsync(s => addresses.Contains(s.Address));

        Dictionary<ulong, long> slotData = pagedData
            .DistinctBy(s => s.Slot)
            .ToDictionary(
                s => s.Slot,
                s => CoinectaUtils.TimeFromSlot(CoinectaUtils.GetNetworkType(configuration), (long)s.Slot)
            );

        return Results.Ok(new { Total = totalCount, Data = pagedData, Extra = new { SlotData = slotData } });
    }

    public async Task<IResult> GetStakeRequestsAsync(int page = 1, int limit = 10)
    {
        using CoinectaDbContext dbContext = dbContextFactory.CreateDbContext();

        int skip = (page - 1) * limit;

        List<StakeRequestByAddress> result = await dbContext.StakeRequestByAddresses
            .Where(s => s.Status == StakeRequestStatus.Pending)
            .OrderBy(s => s.Slot)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();


        return Results.Ok(result);
    }

    public async Task<IResult> GetStakePositionsByStakeKeysAsync(List<string> stakeKeys)
    {
        if (stakeKeys.Count == 0) return Results.BadRequest("No stake keys provided");

        using CoinectaDbContext dbContext = dbContextFactory.CreateDbContext();

        // Current Timestamp
        DateTimeOffset dto = new(DateTime.UtcNow);
        ulong currentTimestamp = (ulong)dto.ToUnixTimeMilliseconds();

        // Get Stake Positions
        List<StakePositionByStakeKey> stakePositions = await dbContext.StakePositionByStakeKeys.Where(s => stakeKeys.Contains(s.StakeKey)).ToListAsync();

        // Transaform Stake Positions
        var result = stakePositions.Select(sp =>
        {
            // Remove NFT
            sp.Amount.MultiAsset.Remove(configuration["CoinectaStakeMintingPolicyId"]!);

            double interest = sp.Interest.Numerator / (double)sp.Interest.Denominator;
            string? policyId = sp.Amount.MultiAsset.Keys.FirstOrDefault();
            Dictionary<string, ulong> asset = sp.Amount.MultiAsset[policyId!];
            string assetName = asset.Keys.FirstOrDefault()!;
            string subject = policyId + assetName;
            ulong total = asset.Values.FirstOrDefault();
            ulong initial = (ulong)(total / (1 + interest));
            ulong bonus = total - initial;
            DateTimeOffset unlockDate = DateTimeOffset.FromUnixTimeMilliseconds((long)sp.LockTime);

            return new
            {
                Subject = subject,
                Total = total,
                UnlockDate = unlockDate,
                Initial = initial,
                Bonus = bonus,
                Interest = interest,
                sp.TxHash,
                sp.TxIndex,
                sp.StakeKey,
            };
        }).OrderByDescending(sp => sp.UnlockDate).ToList();

        return Results.Ok(result);
    }

    public async Task<IResult> GetStakePositionsSnapshotAsync(ulong? slot)
    {
        using CoinectaDbContext dbContext = dbContextFactory.CreateDbContext();

        IQueryable<StakePositionByStakeKey> stakePositionsQuery = dbContext.StakePositionByStakeKeys
            .AsNoTracking();

        if (slot.HasValue)
        {
            stakePositionsQuery = stakePositionsQuery.Where(s => s.Slot <= slot);
        }

        RationalEqualityComparer rationalEqualityComparer = new();

        var stakePositions = await stakePositionsQuery.GroupBy(s => new { s.TxHash, s.TxIndex })
            .Where(g => g.Count() < 2)
            .Select(g => new
            {
                g.First().Interest,
                LockUntil = g.First().LockTime,
                LockedAsset = g.First().StakePosition.Metadata.Data["locked_assets"],
                Expiration = g.First().StakePosition.Metadata.Data["name"].Substring(g.First().StakePosition.Metadata.Data["name"].LastIndexOf('-') + 1).Trim()
            })
            .ToListAsync();

        slot ??= await dbContext.Blocks.OrderByDescending(b => b.Slot).Select(b => b.Slot).FirstOrDefaultAsync();

        long slotTime = SlotUtility.GetPosixTimeSecondsFromSlot(
            CoinectaUtils.SlotUtilityFromNetwork(CoinectaUtils.GetNetworkType(configuration)),
            (long)slot) * 1000;


        var groupedByAsset = stakePositions
            .Select(sp =>
            {
                string[] lockedAssets = sp.LockedAsset!.Trim('[', ']').Trim('(', ')').Split(',');
                Asset asset = new()
                {
                    PolicyId = lockedAssets[0],
                    AssetName = Encoding.UTF8.GetString(Convert.FromHexString(lockedAssets[1].Trim())),
                    Amount = ulong.Parse(lockedAssets[2])
                };

                return new
                {
                    sp.Interest,
                    Asset = asset,
                    sp.LockUntil,
                    sp.Expiration
                };
            })
            .GroupBy(sp => new { sp.Asset.AssetName });

        List<PoolStats> groupedByInterest = groupedByAsset
            .Select(g =>
            {
                var groupedByInterest = g.GroupBy(sp => sp.Interest, rationalEqualityComparer).ToList();
                Dictionary<decimal, int> nftsByInterest = groupedByInterest.ToDictionary(
                    g => (decimal)g.Key.Numerator / g.Key.Denominator,
                    g => g.Count()
                );

                Dictionary<decimal, ulong> rewardsByInterest = groupedByInterest.ToDictionary(
                    g => (decimal)g.Key.Numerator / g.Key.Denominator,
                    g =>
                    {
                        Rational amount = new(g.Aggregate(0UL, (acc, sp) => acc + sp.Asset.Amount), 1);
                        Rational interest = new(g.Key.Denominator, Denominator: g.Key.Numerator + g.Key.Denominator);
                        Rational originalStake = interest * amount;
                        ulong originalStakeAmount = originalStake.Numerator / originalStake.Denominator;
                        ulong amountWithStake = amount.Numerator;
                        return amountWithStake - originalStakeAmount;
                    }
                );

                Dictionary<decimal, StakeData> stakeStatsByInterest = groupedByInterest.ToDictionary(
                    g => (decimal)g.Key.Numerator / g.Key.Denominator,
                    g =>
                    {
                        // Total
                        Rational amount = new(g.Aggregate(0UL, (acc, sp) => acc + sp.Asset.Amount), 1);
                        Rational interest = new(g.Key.Denominator, Denominator: g.Key.Numerator + g.Key.Denominator);
                        Rational originalStakeTotal = interest * amount;
                        ulong totalAmount = originalStakeTotal.Numerator / originalStakeTotal.Denominator;

                        // Locked
                        Rational lockedAmount = new(g.Where(sp => sp.LockUntil > (ulong)slotTime).Aggregate(0UL, (acc, sp) => acc + sp.Asset.Amount), 1);
                        Rational originalLockedStakeTotal = interest * lockedAmount;
                        ulong totalLockedAmount = originalLockedStakeTotal.Numerator / originalLockedStakeTotal.Denominator;

                        // Unclaimed
                        ulong unclaimed = totalAmount - totalLockedAmount;

                        return new StakeData()
                        {
                            Total = totalAmount,
                            Locked = totalLockedAmount,
                            Unclaimed = unclaimed
                        };
                    }
                );

                return new PoolStats()
                {
                    AssetName = g.Key.AssetName,
                    NftsByInterest = nftsByInterest,
                    RewardsByInterest = rewardsByInterest,
                    StakeDataByInterest = stakeStatsByInterest
                };
            })
            .ToList();

        List<PoolStats> groupedByExpiration = groupedByAsset
            .Select(g =>
            {
                var groupedByExpiration = g.GroupBy(sp => sp.Expiration).ToList();
                Dictionary<string, int> nftsByExpiration = groupedByExpiration.ToDictionary(
                    g => g.Key,
                    g => g.Count()
                );

                Dictionary<string, ulong> rewardsByExpiration = groupedByExpiration.ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        return g.GroupBy(sp => sp.Interest, rationalEqualityComparer).ToDictionary(
                            g => (decimal)g.Key.Numerator / g.Key.Denominator,
                            g =>
                            {
                                Rational amount = new(g.Aggregate(0UL, (acc, sp) => acc + sp.Asset.Amount), 1);
                                Rational interest = new(g.Key.Denominator, Denominator: g.Key.Numerator + g.Key.Denominator);
                                Rational originalStake = interest * amount;
                                ulong originalStakeAmount = originalStake.Numerator / originalStake.Denominator;
                                ulong amountWithStake = amount.Numerator;
                                return amountWithStake - originalStakeAmount;
                            }
                        ).Select(g => g.Value).Aggregate(0UL, (acc, rewards) => acc + rewards);
                    }
                );

                Dictionary<string, StakeData> stakeDataByExpiration = groupedByExpiration.ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        Dictionary<decimal, StakeData> groupedByInterest = g.GroupBy(sp => sp.Interest, rationalEqualityComparer).ToDictionary(
                            g => (decimal)g.Key.Numerator / g.Key.Denominator,
                            g =>
                            {
                                // Total
                                Rational amount = new(g.Aggregate(0UL, (acc, sp) => acc + sp.Asset.Amount), 1);
                                Rational interest = new(g.Key.Denominator, Denominator: g.Key.Numerator + g.Key.Denominator);
                                Rational originalStakeTotal = interest * amount;
                                ulong totalAmount = originalStakeTotal.Numerator / originalStakeTotal.Denominator;

                                // Locked
                                Rational lockedAmount = new(g.Where(sp => sp.LockUntil > (ulong)slotTime).Aggregate(0UL, (acc, sp) => acc + sp.Asset.Amount), 1);
                                Rational originalLockedStakeTotal = interest * lockedAmount;
                                ulong totalLockedAmount = originalLockedStakeTotal.Numerator / originalLockedStakeTotal.Denominator;

                                // Unclaimed
                                ulong unclaimed = totalAmount - totalLockedAmount;

                                return new StakeData()
                                {
                                    Total = totalAmount,
                                    Locked = totalLockedAmount,
                                    Unclaimed = unclaimed
                                };
                            }
                        );

                        return new StakeData()
                        {
                            Total = groupedByInterest.Select(g => g.Value.Total).Aggregate(0UL, (acc, total) => acc + total),
                            Locked = groupedByInterest.Select(g => g.Value.Locked).Aggregate(0UL, (acc, total) => acc + total),
                            Unclaimed = groupedByInterest.Select(g => g.Value.Unclaimed).Aggregate(0UL, (acc, total) => acc + total)
                        };
                    }
                );

                return new PoolStats()
                {
                    AssetName = g.Key.AssetName,
                    NftsByExpiration = nftsByExpiration,
                    RewardsByExpiration = rewardsByExpiration,
                    StakeDataByExpiration = stakeDataByExpiration
                };
            })
            .ToList();

        List<PoolStats> result = groupedByInterest.GroupJoin(
            groupedByExpiration,
            gbi => gbi.AssetName,
            gbe => gbe.AssetName,
            (gbi, gbe) =>
            {
                PoolStats? expirationStats = gbe.FirstOrDefault();
                gbi.NftsByExpiration = expirationStats?.NftsByExpiration;
                gbi.RewardsByExpiration = expirationStats?.RewardsByExpiration;
                gbi.StakeDataByExpiration = expirationStats?.StakeDataByExpiration;
                return gbi;
            }
        ).ToList();

        return Results.Ok(result);
    }

    public async Task<IResult> GetAllStakeSnapshotByAddressAync(List<string>? addresses,
    ulong? slot, int? limit, int offset = 0)
    {
        using CoinectaDbContext dbContext = dbContextFactory.CreateDbContext();
        string stakeKeyPrefix = configuration["StakeKeyPrefix"]!;

        IQueryable<NftByAddress> nftsByAddressQuery = dbContext.NftsByAddress
            .AsNoTracking();

        IQueryable<StakePositionByStakeKey> stakePositionByStakeKeysQuery = dbContext.StakePositionByStakeKeys
            .AsNoTracking();

        var querySlot = 0UL;
        if (slot.HasValue)
        {
            querySlot = slot.Value;
        }
        else
        {
            querySlot = await dbContext.Blocks.OrderByDescending(b => b.Slot).Select(b => b.Slot).FirstOrDefaultAsync();
        }

        var stakePositionsByAddress = await nftsByAddressQuery
            .Where(n => n.Slot <= querySlot)
            .GroupBy(n => new { n.TxHash, n.OutputIndex, n.PolicyId, n.AssetName })
            .Where(g => g.Count() < 2)
            .Select(g => new
            {
                Key = string.Concat(g.First().PolicyId, g.First().AssetName.Substring(stakeKeyPrefix.Length)),
                g.First().Address
            })
            .Join(dbContext.StakePositionByStakeKeys, n => n.Key, s => s.StakeKey, (n, s) => new
            {
                n.Address,
                s.Interest,
                Amount = s.Amount.MultiAsset.Values.Last().Values.First()
            })
            .GroupBy(s => s.Address)
            .ToListAsync();

        var result = stakePositionsByAddress
            .Select(sp =>
            {
                ulong totalStake = sp.Select(s => s.Amount).Aggregate(0UL, (acc, stake) => acc + stake);

                return new
                {
                    Address = sp.Key,
                    UniqueNfts = sp.Count(),
                    TotalStake = totalStake,
                    CummulativeWeight = CoinectaUtils.CalculateTotalWeight(totalStake)
                };
            })
            .ToList();

        var totalStake = result.Select(r => r.TotalStake).Aggregate(0UL, (acc, stake) => acc + stake);
        var totalStakers = result.Count;
        var totalCummulativeWeight = result.Select(r => r.CummulativeWeight).Aggregate(0UL, (acc, weight) => acc + (ulong)weight);

        if (addresses is not null && addresses.Count > 0)
        {
            result = result.Where(r => addresses.Contains(r.Address)).ToList();
        }

        result = result.Skip(offset).ToList();

        if (limit.HasValue)
        {
            result = result.Take(limit.Value).ToList();
        }

        return Results.Ok(new
        {
            Data = result,
            TotalStakers = totalStakers,
            TotalStake = totalStake,
            TotalCummulativeWeight = totalCummulativeWeight
        });
    }
}
