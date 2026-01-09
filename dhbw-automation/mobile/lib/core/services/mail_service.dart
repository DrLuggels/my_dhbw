import '../config/api_config.dart';
import '../models/api_response.dart';
import '../models/email.dart';
import 'api_client.dart';

class MailService {
  final ApiClient _apiClient;

  MailService(this._apiClient);

  Future<List<Email>> getInbox({
    String? folder,
    bool? isRead,
    bool? requiresAction,
  }) async {
    try {
      final queryParams = <String, dynamic>{};
      if (folder != null) queryParams['folder'] = folder;
      if (isRead != null) queryParams['isRead'] = isRead;
      if (requiresAction != null) queryParams['requiresAction'] = requiresAction;

      final response = await _apiClient.get(
        '${ApiConfig.mail}/inbox',
        queryParameters: queryParams,
      );

      final apiResponse = ApiResponse<List<dynamic>>.fromJson(
        response.data,
        (json) => json as List<dynamic>,
      );

      if (!apiResponse.success || apiResponse.data == null) {
        return [];
      }

      return apiResponse.data!.map((e) => Email.fromJson(e)).toList();
    } catch (e) {
      print('MailService.getInbox error: $e');
      return [];
    }
  }

  Future<void> markAsRead(int emailId) async {
    try {
      await _apiClient.put('${ApiConfig.mail}/$emailId/read');
    } catch (e) {
      print('MailService.markAsRead error: $e');
      rethrow;
    }
  }

  Future<void> performAction(int emailId, String action) async {
    try {
      await _apiClient.post(
        '${ApiConfig.mail}/$emailId/action',
        data: {'action': action},
      );
    } catch (e) {
      print('MailService.performAction error: $e');
      rethrow;
    }
  }

  Future<void> syncMail() async {
    try {
      await _apiClient.post('${ApiConfig.mail}/sync');
    } catch (e) {
      print('MailService.syncMail error: $e');
      rethrow;
    }
  }
}
