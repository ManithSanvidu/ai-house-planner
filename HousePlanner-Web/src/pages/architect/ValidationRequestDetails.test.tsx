import { render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, test, expect, beforeEach, vi } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ValidationRequestDetails from './ValidationRequestDetails';
import { validationRequestService } from '../../services/validationRequestService';
import { workflowService } from '../../services/workflowService';

vi.mock('../../services/validationRequestService');
vi.mock('../../services/workflowService');

describe('ValidationRequestDetails', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (workflowService.getDesignVisualization as any).mockResolvedValue({ status: 'not_started' });
  });

  const renderComponent = () => {
    return render(
      <MemoryRouter initialEntries={['/architect/requests/123']}>
        <Routes>
          <Route path="/architect/requests/:id" element={<ValidationRequestDetails />} />
        </Routes>
      </MemoryRouter>
    );
  };

  test('Test A: Renders PASS validation summary when validationResult.passed is true', async () => {
    (validationRequestService.getById as any).mockResolvedValue({
      id: '123',
      status: 'Pending',
      clientName: 'Test Client',
      approvalEligibility: { canApprove: true, budgetStatus: 'within_budget', rulesPassed: true },
      cost: null,
      validationResult: {
        passed: true,
        summary: 'All good',
        rules: [
          { ruleName: 'bedroom_count', passed: true, expected: 2, actual: 2, reason: null }
        ]
      }
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('Planning / Feasibility Validation')).toBeInTheDocument();
    });
    const passes = screen.getAllByText('PASS');
    expect(passes.length).toBeGreaterThan(0);
    expect(screen.getByText('bedroom count')).toBeInTheDocument();
    expect(screen.getByText('Expected:')).toBeInTheDocument();
    expect(screen.getByText('Actual:')).toBeInTheDocument();
  });

  test('Test B: Renders FAIL validation summary when validationResult.passed is false', async () => {
    (validationRequestService.getById as any).mockResolvedValue({
      id: '123',
      status: 'Pending',
      clientName: 'Test Client',
      approvalEligibility: { canApprove: true, budgetStatus: 'within_budget', rulesPassed: true },
      cost: null,
      validationResult: {
        passed: false,
        summary: 'Failed constraints',
        rules: [
          { ruleName: 'budget', passed: false, expected: 100000, actual: 150000, reason: 'Over budget' }
        ]
      }
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('Planning / Feasibility Validation')).toBeInTheDocument();
    });
    
    // There might be multiple FAIL text (overall and rule)
    const fails = screen.getAllByText('FAIL');
    expect(fails.length).toBeGreaterThan(0);
    expect(screen.getByText('Over budget')).toBeInTheDocument();
  });

  test('Test C: Renders fallback message when validationResult is null', async () => {
    (validationRequestService.getById as any).mockResolvedValue({
      id: '123',
      status: 'Pending',
      clientName: 'Test Client',
      approvalEligibility: { canApprove: true, budgetStatus: 'within_budget', rulesPassed: true },
      cost: null,
      validationResult: null
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('Planning / Feasibility Validation')).toBeInTheDocument();
    });
    expect(screen.getByText(/Detailed validation evidence is not available for this older workflow/i)).toBeInTheDocument();
  });

  test('Test D: Approval controls still render', async () => {
    (validationRequestService.getById as any).mockResolvedValue({
      id: '123',
      status: 'Pending',
      clientName: 'Test Client',
      approvalEligibility: { canApprove: true, budgetStatus: 'within_budget', rulesPassed: true },
      cost: null,
      validationResult: null
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Approve Design/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /Reject \/ Request Changes/i })).toBeInTheDocument();
    });
  });

  test('Test E: Renders NOT APPLICABLE status when rule status is NOT_APPLICABLE', async () => {
    (validationRequestService.getById as any).mockResolvedValue({
      id: '123',
      status: 'Pending',
      clientName: 'Test Client',
      approvalEligibility: { canApprove: true, budgetStatus: 'within_budget', rulesPassed: true },
      cost: null,
      validationResult: {
        passed: true,
        summary: 'All good',
        rules: [
          { ruleName: 'budget', passed: true, status: 'NOT_APPLICABLE', expected: 'none', actual: 'none', reason: 'Skipped' }
        ]
      }
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('Planning / Feasibility Validation')).toBeInTheDocument();
    });
    const notApp = screen.getAllByText('NOT APPLICABLE');
    expect(notApp.length).toBeGreaterThan(0);
  });
});
