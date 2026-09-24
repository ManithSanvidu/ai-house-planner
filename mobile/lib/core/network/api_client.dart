import 'package:dio/dio.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class ApiClient {
  static final Dio _dio = Dio(
    BaseOptions(
      baseUrl: dotenv.env['API_BASE_URL'] ?? 'http://localhost:5265/api/v1',
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 60),
    ),
  )..interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        const storage = FlutterSecureStorage();
        final token = await storage.read(key: 'auth_token');
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        return handler.next(options);
      },
    ));

  static Dio get instance => _dio;
}
