# 0004: Community-specific floor plan pricing

**Status:** Accepted (2026-11-02)

## Context

A floor plan is offered in one or more communities. Real homebuilders usually price the same plan differently in each community because land cost, lot size, and local market conditions vary. A single `BasePrice` on `FloorPlan` can't express this.

## Decision

Model the plan-to-community relationship as an explicit join entity, `CommunityFloorPlan`, which carries the community-specific base price. `FloorPlan` doesn't store a price itself. Plan search results show the price for that community ("from $XXX,XXX").

## Alternatives considered

- **Single base price on `FloorPlan`:** simpler, but unrealistic. Searches like "under $450K near Frederick" would return wrong results for plans priced differently by market.
- **EF Core skip navigation (implicit join table):** less code, but can't hold extra columns such as price.
