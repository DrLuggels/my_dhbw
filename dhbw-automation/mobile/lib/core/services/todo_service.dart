import '../config/api_config.dart';
import '../models/api_response.dart';
import '../models/todo.dart';
import 'api_client.dart';

class TodoService {
  final ApiClient _apiClient;

  TodoService(this._apiClient);

  Future<List<Todo>> getTodos({
    String? status,
    String? category,
    int? priority,
  }) async {
    try {
      final queryParams = <String, dynamic>{};
      if (status != null) queryParams['status'] = status;
      if (category != null) queryParams['category'] = category;
      if (priority != null) queryParams['priority'] = priority;

      final response = await _apiClient.get(
        ApiConfig.todos,
        queryParameters: queryParams,
      );

      final apiResponse = ApiResponse<List<dynamic>>.fromJson(
        response.data,
        (json) => json as List<dynamic>,
      );

      if (!apiResponse.success || apiResponse.data == null) {
        return [];
      }

      return apiResponse.data!.map((e) => Todo.fromJson(e)).toList();
    } catch (e) {
      print('TodoService.getTodos error: $e');
      return [];
    }
  }

  Future<Todo> createTodo({
    required String title,
    String? description,
    DateTime? dueDate,
    required String category,
    int priority = 5,
  }) async {
    try {
      final response = await _apiClient.post(
        ApiConfig.todos,
        data: {
          'title': title,
          'description': description,
          'dueDate': dueDate?.toIso8601String(),
          'category': category,
          'priority': priority,
        },
      );

      final apiResponse = ApiResponse<Map<String, dynamic>>.fromJson(
        response.data,
        (json) => json as Map<String, dynamic>,
      );

      if (!apiResponse.success || apiResponse.data == null) {
        throw Exception('Todo konnte nicht erstellt werden');
      }

      return Todo.fromJson(apiResponse.data!);
    } catch (e) {
      print('TodoService.createTodo error: $e');
      rethrow;
    }
  }

  Future<void> updateTodoStatus(int todoId, String status) async {
    try {
      await _apiClient.patch(
        '${ApiConfig.todos}/$todoId/status',
        data: {'status': status},
      );
    } catch (e) {
      print('TodoService.updateTodoStatus error: $e');
      rethrow;
    }
  }

  Future<void> deleteTodo(int todoId) async {
    try {
      await _apiClient.delete('${ApiConfig.todos}/$todoId');
    } catch (e) {
      print('TodoService.deleteTodo error: $e');
      rethrow;
    }
  }
}
