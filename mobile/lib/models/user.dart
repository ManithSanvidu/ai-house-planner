class User {
  final String id;
  final String email;
  final String role;
  final String? token;
  final String? fullName;

  User({
    required this.id,
    required this.email,
    required this.role,
    this.token,
    this.fullName,
  });

  factory User.fromJson(Map<String, dynamic> json) {
    return User(
      id: json['id'] ?? json['uid'] ?? '',
      email: json['email'] ?? '',
      role: json['role'] ?? 'User',
      token: json['token'],
      fullName: json['fullName'] ?? json['full_name'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'email': email,
      'role': role,
      'token': token,
      'fullName': fullName,
    };
  }
}
