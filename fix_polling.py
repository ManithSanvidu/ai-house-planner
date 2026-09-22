import sys

with open("HousePlanner-Web/src/pages/WorkflowReviewPage.tsx", "r") as f:
    content = f.read()

target = """    let interval: ReturnType<typeof setInterval>;

    const fetchWorkflow = async () => {
      try {
        const data = await workflowService.getWorkflowStatus(id, previewDesignId);
        setWorkflow(data);
        setError(null);
        setLoading(false);

        // If the status is no longer running, we can stop polling
        if (data.status !== 'running' && data.status !== 'pending') {
          clearInterval(interval);
        }
      } catch (err: any) {
        if (err.response?.status === 404 || err.message?.includes('404')) {
          setError(null);
        } else {
          setError(err.message || 'Failed to fetch workflow status');
          setLoading(false);
          clearInterval(interval);
        }
      }
    };"""

replacement = """    let interval: ReturnType<typeof setInterval>;
    let error404Count = 0;

    const fetchWorkflow = async () => {
      try {
        const data = await workflowService.getWorkflowStatus(id, previewDesignId);
        setWorkflow(data);
        setError(null);
        setLoading(false);
        error404Count = 0;

        // If the status is no longer running, we can stop polling
        if (data.status !== 'running' && data.status !== 'pending') {
          clearInterval(interval);
        }
      } catch (err: any) {
        if (err.response?.status === 404 || err.message?.includes('404')) {
          error404Count++;
          if (error404Count >= 5) {
            setError('Workflow not found. It may have failed to save or you do not have permission.');
            setLoading(false);
            clearInterval(interval);
          } else {
            setError(null);
          }
        } else {
          setError(err.message || 'Failed to fetch workflow status');
          setLoading(false);
          clearInterval(interval);
        }
      }
    };"""

if target in content:
    content = content.replace(target, replacement)
    with open("HousePlanner-Web/src/pages/WorkflowReviewPage.tsx", "w") as f:
        f.write(content)
    print("Replaced successfully")
else:
    print("Target not found")
