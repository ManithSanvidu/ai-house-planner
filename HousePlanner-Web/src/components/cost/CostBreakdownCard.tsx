import type { CostSummaryDto } from '../../services/workflowService';
import Card from '../common/Card';

interface CostBreakdownCardProps {
  cost: CostSummaryDto | null;
}

const formatLkr = (value: number) => `LKR ${value.toLocaleString()}`;

const hasValidCostSummary = (cost: CostSummaryDto | null): cost is CostSummaryDto =>
  cost !== null
  && Number.isFinite(cost.materialCostLkr)
  && Number.isFinite(cost.labourCostLkr)
  && Number.isFinite(cost.totalCostLkr)
  && Number.isFinite(cost.budgetDeltaPercent);

export const CostBreakdownCard = ({ cost }: CostBreakdownCardProps) => {
  if (!hasValidCostSummary(cost)) {
    return (
      <Card title="Cost Estimate" subtitle="Current construction cost breakdown for this design.">
        <div className="rounded-lg border border-zinc-200 bg-zinc-50 px-5 py-8 text-center">
          <p className="text-sm text-zinc-600">Cost estimate is not available yet.</p>
        </div>
      </Card>
    );
  }

  const budgetPercentage = cost.budgetDeltaPercent;
  const progressWidth = Math.min(Math.max(budgetPercentage, 0), 100);
  const budgetStatus = budgetPercentage < 100
    ? { label: 'Within budget', classes: 'bg-emerald-50 text-emerald-700 border-emerald-200' }
    : budgetPercentage === 100
      ? { label: 'At budget', classes: 'bg-amber-50 text-amber-700 border-amber-200' }
      : { label: 'Over budget', classes: 'bg-red-50 text-red-700 border-red-200' };
  const formattedPercentage = budgetPercentage.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

  const summaryItems = [
    { label: 'Material Cost', value: formatLkr(cost.materialCostLkr) },
    { label: 'Labour Cost', value: formatLkr(cost.labourCostLkr) },
    { label: 'Total Estimated Cost', value: formatLkr(cost.totalCostLkr), emphasized: true },
    { label: 'Budget Used %', value: `${formattedPercentage}%`, emphasized: true },
  ];

  return (
    <Card title="Cost Estimate" subtitle="Current construction cost breakdown for this design.">
      <div className="space-y-6">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {summaryItems.map((item) => (
            <div
              key={item.label}
              className={`rounded-xl border p-4 ${
                item.emphasized
                  ? 'border-indigo-100 bg-indigo-50/60'
                  : 'border-zinc-200 bg-zinc-50'
              }`}
            >
              <p className="text-xs font-semibold uppercase tracking-wider text-zinc-500">
                {item.label}
              </p>
              <p className={`mt-2 text-lg font-semibold ${item.emphasized ? 'text-indigo-950' : 'text-zinc-900'}`}>
                {item.value}
              </p>
            </div>
          ))}
        </div>

        <div className="rounded-xl border border-zinc-200 bg-white p-4">
          <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
            <div>
              <p className="text-sm font-semibold text-zinc-900">Budget usage</p>
              <p className="mt-0.5 text-xs text-zinc-500">{formattedPercentage}% of the project budget</p>
            </div>
            <span className={`rounded-full border px-2.5 py-1 text-xs font-semibold ${budgetStatus.classes}`}>
              {budgetStatus.label}
            </span>
          </div>

          <div
            className="h-2.5 overflow-hidden rounded-full bg-zinc-100"
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
      </div>
    </Card>
  );
};

export default CostBreakdownCard;
