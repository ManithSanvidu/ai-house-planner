import { render, screen } from '@testing-library/react';
import { AgentTimeline } from './AgentTimeline';
import type { AgentExecutionLogEntry } from '../services/workflowService';
import { describe, it, expect } from 'vitest';

describe('AgentTimeline Component', () => {
  it('renders completed agents', () => {
    const log: AgentExecutionLogEntry[] = [
      { agent: 'coordinator', status: 'completed', message: 'Workflow initialised' },
      { agent: 'requirement_analysis', status: 'completed', message: 'Extracted user requirements' }
    ];

    render(<AgentTimeline agentExecutionLog={log} />);

    expect(screen.getByText('AI Design Process')).toBeTruthy();
    
    // Check formatted agent names
    expect(screen.getByText('Coordinator')).toBeTruthy();
    expect(screen.getByText('Workflow initialised')).toBeTruthy();
    
    expect(screen.getByText('Requirement Analysis')).toBeTruthy();
    expect(screen.getByText('Extracted user requirements')).toBeTruthy();
  });

  it('renders failed agent', () => {
    const log: AgentExecutionLogEntry[] = [
      { agent: 'design', status: 'completed', message: 'Selected validated catalogue plan' },
      { agent: 'validation', status: 'failed', message: 'Geometry and rules failed' }
    ];

    render(<AgentTimeline agentExecutionLog={log} />);

    expect(screen.getByText('Validation')).toBeTruthy();
    expect(screen.getByText('Geometry and rules failed')).toBeTruthy();
    
    // Check if the fail styling is applied (we can check class on the parent, or just trust the text)
    const failedAgentName = screen.getByText('Validation');
    expect(failedAgentName.className).toContain('text-red-600');
  });

  it('updates when polling data changes (renders new props)', () => {
    const { rerender } = render(<AgentTimeline agentExecutionLog={null} />);
    
    // initially null, renders nothing
    expect(screen.queryByText('AI Design Process')).toBeNull();

    const log1: AgentExecutionLogEntry[] = [
      { agent: 'coordinator', status: 'completed', message: 'Workflow initialised' }
    ];
    
    rerender(<AgentTimeline agentExecutionLog={log1} />);
    expect(screen.getByText('Coordinator')).toBeTruthy();

    const log2: AgentExecutionLogEntry[] = [
      { agent: 'coordinator', status: 'completed', message: 'Workflow initialised' },
      { agent: 'rendering', status: 'running', message: 'Preparing visual output' }
    ];
    
    rerender(<AgentTimeline agentExecutionLog={log2} />);
    expect(screen.getByText('Rendering')).toBeTruthy();
    expect(screen.getByText('Preparing visual output')).toBeTruthy();
  });
});
