import type { CostSummaryDto } from '../../services/workflowService';
import Card from '../common/Card';

interface CostBreakdownCardProps {
 cost: CostSummaryDto | null;
 hideTitle?: boolean;
}

const formatLkr = (value: number) => `LKR ${value.toLocaleString()}`;
const formatNumber = (value: number, maximumFractionDigits = 2) =>
 value.toLocaleString(undefined, { maximumFractionDigits });

const hasValidCostSummary = (cost: CostSummaryDto | null): cost is CostSummaryDto =>
 cost !== null
 && Number.isFinite(cost.materialCostLkr)
 && Number.isFinite(cost.labourCostLkr)
 && Number.isFinite(cost.totalCostLkr);

export const CostBreakdownCard = ({ cost, hideTitle }: CostBreakdownCardProps) => {
 if (!hasValidCostSummary(cost)) {
  return (
   <Card title={hideTitle ? undefined : "Cost Estimate"} subtitle={hideTitle ? undefined : "Current construction cost breakdown for this design."}>
    <div className="rounded-lg border border-border bg-surface-elevated px-5 py-8 text-center">
     <p className="text-sm text-text-secondary">Cost estimate is not available yet.</p>
    </div>
   </Card>
  );
 }

 const budgetPercentage = cost.budgetDeltaPercent;
 const hasBudgetComparison = budgetPercentage !== null && Number.isFinite(budgetPercentage);
 const progressWidth = hasBudgetComparison ? Math.min(Math.max(budgetPercentage, 0), 100) : 0;
 const budgetStatus = hasBudgetComparison
  ? budgetPercentage < 100
   ? { label: 'Within budget', classes: 'bg-emerald-50 text-emerald-700 border-emerald-200' }
   : budgetPercentage === 100
    ? { label: 'At budget', classes: 'bg-amber-50 text-amber-700 border-amber-200' }
    : { label: 'Over budget', classes: 'bg-red-50 text-red-700 border-red-200' }
  : null;
 const formattedPercentage = hasBudgetComparison
  ? budgetPercentage.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  : null;

 const summaryItems = [
  { label: 'Material Cost', value: formatLkr(cost.materialCostLkr) },
  { label: 'Labour Cost', value: formatLkr(cost.labourCostLkr) },
  { label: 'Total Estimated Cost', value: formatLkr(cost.totalCostLkr), emphasized: true },
  ...(hasBudgetComparison
   ? [{ label: 'Budget Used %', value: `${formattedPercentage}%`, emphasized: true }]
   : []),
 ];
 const breakdown = cost.breakdown?.filter((item) =>
  Number.isFinite(item.amountLkr) && Number.isFinite(item.sharePercent)) ?? [];

 const calculationBasis = (item: NonNullable<CostSummaryDto['breakdown']>[number]) => {
  if (item.category === 'labour' || item.unit === 'factor' || item.unit === 'ratio') {
   return `${formatNumber(item.unitCostLkr)} × material cost`;
  }

  const terrain = item.terrainMultiplier === 1
   ? ''
   : ` × ${formatNumber(item.terrainMultiplier)} terrain`;
  return `${formatLkr(item.unitCostLkr)}/sq ft × ${formatNumber(item.appliedQuantity)} sq ft${terrain}`;
 };

 return (
  <Card title={hideTitle ? undefined : "Cost Estimate"} subtitle={hideTitle ? undefined : "Current construction cost breakdown for this design."}>
   <div className="space-y-6">
    <div className="flex flex-wrap gap-x-6 gap-y-2 rounded-lg border border-border bg-surface-elevated px-4 py-3 text-xs text-text-secondary">
     {cost.formulaVersion && <span>Method: <strong>{cost.formulaVersion}</strong></span>}
     {cost.appliedAreaSqft != null && <span>Applied area: <strong>{formatNumber(cost.appliedAreaSqft)} sq ft</strong></span>}
     {cost.terrainType && <span>Terrain: <strong className="capitalize">{cost.terrainType}</strong></span>}
     {cost.estimatedAt && <span>Estimated: <strong>{new Date(cost.estimatedAt).toLocaleDateString()}</strong></span>}
    </div>
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
     {summaryItems.map((item) => (
      <div
       key={item.label}
       className={`rounded-xl border p-4 ${
        item.emphasized
         ? 'border-indigo-100 bg-indigo-50/60'
         : 'border-border bg-surface-elevated'
       }`}
      >
       <p className="text-xs font-semibold uppercase tracking-wider text-text-muted">
        {item.label}
       </p>
       <p className={`mt-2 text-lg font-semibold ${item.emphasized ? 'text-indigo-950' : 'text-zinc-900'}`}>
        {item.value}
       </p>
      </div>
     ))}
    </div>

    {breakdown.length > 0 && (
     <div>
      <div className="mb-3">
       <h3 className="text-sm font-semibold uppercase tracking-wider text-zinc-700">
        Cost-head breakdown
       </h3>
       <p className="mt-1 text-xs text-text-muted">
        Calculated from the pricing snapshot saved with this design.
       </p>
      </div>

      <div className="overflow-x-auto rounded-xl border border-border bg-surface">
       <table className="min-w-[720px] w-full border-collapse text-left">
        <thead className="bg-surface-elevated text-xs uppercase tracking-wider text-text-muted">
         <tr>
          <th className="px-4 py-3 font-semibold">Cost head</th>
          <th className="px-4 py-3 font-semibold">Calculation basis</th>
          <th className="px-4 py-3 text-right font-semibold">Share</th>
          <th className="px-4 py-3 text-right font-semibold">Amount</th>
         </tr>
        </thead>
        <tbody className="divide-y divide-zinc-200">
         {breakdown.map((item) => (
          <tr key={`${item.category}-${item.itemName}`} className="align-top">
           <td className="px-4 py-3">
            <p className="text-sm font-semibold text-zinc-900">{item.costHead}</p>
            <p className="mt-0.5 text-xs text-text-muted">{item.itemName}</p>
            {(item.provider || item.sourceReference || item.pricingUpdatedAt) && (
             <p className="mt-1 max-w-xs text-[11px] text-text-secondary">
              {[item.provider, item.sourceReference,
               item.pricingUpdatedAt ? `updated ${new Date(item.pricingUpdatedAt).toLocaleDateString()}` : null]
               .filter(Boolean).join(' · ')}
             </p>
            )}
           </td>
           <td className="px-4 py-3 text-xs text-text-secondary">
            {calculationBasis(item)}
           </td>
           <td className="px-4 py-3 text-right text-sm text-text-secondary">
            {formatNumber(item.sharePercent)}%
           </td>
           <td className="whitespace-nowrap px-4 py-3 text-right text-sm font-semibold text-zinc-900">
            {formatLkr(item.amountLkr)}
           </td>
          </tr>
         ))}
        </tbody>
        <tfoot className="border-t border-zinc-300 bg-indigo-50/60">
         <tr>
          <td className="px-4 py-3 text-sm font-bold text-indigo-950" colSpan={2}>
           Total estimated cost
          </td>
          <td className="px-4 py-3 text-right text-sm font-bold text-indigo-950">100%</td>
          <td className="whitespace-nowrap px-4 py-3 text-right text-sm font-bold text-indigo-950">
           {formatLkr(cost.totalCostLkr)}
          </td>
         </tr>
        </tfoot>
       </table>
      </div>
     </div>
    )}

    {hasBudgetComparison && budgetStatus && formattedPercentage && (
     <div className="rounded-xl border border-border bg-surface p-4">
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
       <div>
        <p className="text-sm font-semibold text-zinc-900">Budget usage</p>
        <p className="mt-0.5 text-xs text-text-muted">{formattedPercentage}% of the project budget</p>
       </div>
       <span className={`rounded-full border px-2.5 py-1 text-xs font-semibold ${budgetStatus.classes}`}>
        {budgetStatus.label}
       </span>
      </div>

      <div
       className="h-2.5 overflow-hidden rounded-full bg-surface-muted"
       role="progressbar"
       aria-label="Budget used"
       aria-valuemin={0}
       aria-valuemax={100}
       aria-valuenow={progressWidth}
       aria-valuetext={`${formattedPercentage}% ${budgetStatus.label.toLowerCase()}`}
      >
       <div
        className={`h-full rounded-full transition-[width] duration-300 ${
         budgetPercentage > 100 ? 'bg-red-500' : 'bg-indigo-600'
        }`}
        style={{ width: `${progressWidth}%` }}
       />
      </div>
     </div>
    )}
    <p className="text-xs leading-5 text-text-muted">
     Preliminary category-level estimate based on the pricing snapshot saved with this design. It is not a final quotation or itemized bill of quantities.
    </p>
   </div>
  </Card>
 );
};

export default CostBreakdownCard;
