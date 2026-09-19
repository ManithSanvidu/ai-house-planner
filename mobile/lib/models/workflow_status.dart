class WorkflowStatus {
  final String workflowId;
  final String status;
  final String approvalStatus;
  final String? failureReason;
  // Additional complex objects omitted for simplicity here, but can be added if needed
  
  WorkflowStatus({
    required this.workflowId,
    required this.status,
    required this.approvalStatus,
    this.failureReason,
  });

  factory WorkflowStatus.fromJson(Map<String, dynamic> json) {
    return WorkflowStatus(
      workflowId: json['workflowId'] ?? '',
      status: json['status'] ?? 'unknown',
      approvalStatus: json['approvalStatus'] ?? 'pending',
      failureReason: json['failureReason'],
    );
  }
}
