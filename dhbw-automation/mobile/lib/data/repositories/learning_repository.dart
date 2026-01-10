import 'dart:math';
import 'package:dio/dio.dart';
import 'package:hive/hive.dart';
import '../../core/network/dio_client.dart';
import '../../core/network/api_response.dart';
import '../../core/constants/api_constants.dart';
import '../models/learning/exercise_model.dart';
import '../local/hive_boxes.dart';

/// Learning Repository
/// Handles exercises with spaced repetition (SM-2 algorithm) and offline support
class LearningRepository {
  final DioClient _dioClient;
  final Box<ExerciseModel> _exercisesBox;

  LearningRepository(this._dioClient)
      : _exercisesBox = HiveBoxes().getExercisesBox();

  /// Get due exercises (due today or overdue)
  Future<ApiResponse<List<ExerciseModel>>> getDueExercises(
    int userId,
  ) async {
    try {
      final response = await _dioClient.get(
        '${ApiConstants.dueExercises}/$userId',
      );

      final apiResponse = ApiResponse.fromJson(
        response.data,
        (json) => (json as List)
            .map((item) => ExerciseModel.fromJson(item as Map<String, dynamic>))
            .toList(),
      );

      // Update Hive cache
      if (apiResponse.success && apiResponse.data != null) {
        for (var exercise in apiResponse.data!) {
          await _exercisesBox.put(exercise.id, exercise);
        }
      }

      return apiResponse;
    } on DioException catch (e) {
      // Offline fallback: return cached due exercises
      if (e.type == DioExceptionType.connectionError ||
          e.type == DioExceptionType.connectionTimeout) {
        final now = DateTime.now();
        final cachedExercises = _exercisesBox.values
            .where((ex) =>
                ex.userId == userId && ex.nextReviewDate.isBefore(now))
            .toList();

        return ApiResponse(
          success: true,
          data: cachedExercises,
          message: 'Offline: ${cachedExercises.length} fällige Übungen aus Cache',
        );
      }
      throw _handleError(e);
    }
  }

  /// Get all exercises for user
  Future<ApiResponse<List<ExerciseModel>>> getAllExercises(
    int userId,
  ) async {
    try {
      final response = await _dioClient.get(
        '${ApiConstants.userExercises}/$userId',
      );

      final apiResponse = ApiResponse.fromJson(
        response.data,
        (json) => (json as List)
            .map((item) => ExerciseModel.fromJson(item as Map<String, dynamic>))
            .toList(),
      );

      // Update Hive cache
      if (apiResponse.success && apiResponse.data != null) {
        await _exercisesBox.clear();
        for (var exercise in apiResponse.data!) {
          await _exercisesBox.put(exercise.id, exercise);
        }
      }

      return apiResponse;
    } on DioException catch (e) {
      // Offline fallback
      if (e.type == DioExceptionType.connectionError ||
          e.type == DioExceptionType.connectionTimeout) {
        final cachedExercises =
            _exercisesBox.values.where((ex) => ex.userId == userId).toList();

        return ApiResponse(
          success: true,
          data: cachedExercises,
          message: 'Offline: ${cachedExercises.length} Übungen aus Cache',
        );
      }
      throw _handleError(e);
    }
  }

  /// Submit answer with quality rating (0-5 for SM-2)
  /// Quality: 0 = complete blackout, 5 = perfect recall
  Future<ApiResponse<ExerciseModel>> submitAnswer(
    int exerciseId,
    String answer,
    int quality,
  ) async {
    try {
      final response = await _dioClient.post(
        '${ApiConstants.submitAnswer}/$exerciseId',
        data: {
          'answer': answer,
          'quality': quality,
        },
      );

      final apiResponse = ApiResponse.fromJson(
        response.data,
        (json) => ExerciseModel.fromJson(json as Map<String, dynamic>),
      );

      // Update Hive cache
      if (apiResponse.success && apiResponse.data != null) {
        await _exercisesBox.put(exerciseId, apiResponse.data!);
      }

      return apiResponse;
    } on DioException catch (e) {
      // Offline: Calculate locally with SM-2
      if (e.type == DioExceptionType.connectionError ||
          e.type == DioExceptionType.connectionTimeout) {
        final cachedExercise = _exercisesBox.get(exerciseId);
        if (cachedExercise != null) {
          final updatedExercise = _calculateSM2(cachedExercise, quality);
          await _exercisesBox.put(exerciseId, updatedExercise);

          return ApiResponse(
            success: true,
            data: updatedExercise,
            message: 'Offline: Antwort lokal gespeichert',
          );
        }
      }
      throw _handleError(e);
    }
  }

  /// SM-2 Spaced Repetition Algorithm
  /// Based on SuperMemo SM-2 algorithm
  /// https://www.supermemo.com/en/archives1990-2015/english/ol/sm2
  ExerciseModel _calculateSM2(ExerciseModel exercise, int quality) {
    // Clamp quality to 0-5 range
    final q = quality.clamp(0, 5);

    // Calculate new ease factor
    // EF' = EF + (0.1 - (5 - q) * (0.08 + (5 - q) * 0.02))
    double newEaseFactor = exercise.easeFactor +
        (0.1 - (5 - q) * (0.08 + (5 - q) * 0.02));

    // Ease factor should not drop below 1.3
    newEaseFactor = max(1.3, newEaseFactor);

    int newRepetitions = exercise.repetitions;
    int intervalDays;

    if (q < 3) {
      // Incorrect recall: reset
      newRepetitions = 0;
      intervalDays = 1;
    } else {
      // Correct recall: increase interval
      newRepetitions++;

      if (newRepetitions == 1) {
        intervalDays = 1;
      } else if (newRepetitions == 2) {
        intervalDays = 6;
      } else {
        // Calculate interval based on previous interval and ease factor
        // For simplicity, we use a base of 6 days * EF^(repetitions-2)
        intervalDays =
            (6 * pow(newEaseFactor, newRepetitions - 2)).round();
      }
    }

    // Calculate next review date
    final nextReviewDate = DateTime.now().add(Duration(days: intervalDays));

    return exercise.copyWith(
      easeFactor: newEaseFactor,
      repetitions: newRepetitions,
      reviewCount: exercise.reviewCount + 1,
      nextReviewDate: nextReviewDate,
      lastAnswer: '',
      lastReviewedAt: DateTime.now(),
    );
  }

  /// Handle Dio errors
  Exception _handleError(DioException e) {
    if (e.response != null) {
      final data = e.response!.data;
      String errorMessage = 'Ein Fehler ist aufgetreten';

      if (data is Map<String, dynamic>) {
        if (data['message'] != null) {
          errorMessage = data['message'];
        } else if (data['errors'] != null && data['errors'] is List) {
          errorMessage = (data['errors'] as List).join(', ');
        }
      }

      return Exception(errorMessage);
    } else if (e.type == DioExceptionType.connectionTimeout ||
        e.type == DioExceptionType.receiveTimeout) {
      return Exception('Zeitüberschreitung der Verbindung');
    } else if (e.type == DioExceptionType.connectionError) {
      return Exception('Keine Verbindung zum Server möglich');
    }

    return Exception('Netzwerkfehler: ${e.message}');
  }
}
