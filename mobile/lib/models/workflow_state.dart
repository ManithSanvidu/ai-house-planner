class WorkflowState {
  final String workflowId;
  final String status;
  final String approvalStatus;
  final String? failureReason;
  final Map<String, dynamic>? design;
  final Map<String, dynamic>? constructionPlan;

  WorkflowState({
    required this.workflowId,
    required this.status,
    required this.approvalStatus,
    this.failureReason,
    this.design,
    this.constructionPlan,
  });

  factory WorkflowState.fromJson(Map<String, dynamic> json) {
    return WorkflowState(
      workflowId: json['workflowId'] ?? '',
      status: json['status'] ?? 'unknown',
      approvalStatus: json['approvalStatus'] ?? 'pending',
      failureReason: json['failureReason'],
      design: json['design'],
      constructionPlan: json['constructionPlan'],
    );
  }
}
