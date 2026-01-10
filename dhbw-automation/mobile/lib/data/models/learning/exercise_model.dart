import 'package:freezed_annotation/freezed_annotation.dart';
import 'package:hive/hive.dart';

part 'exercise_model.freezed.dart';
part 'exercise_model.g.dart';

/// Exercise Model with Hive support for offline caching and spaced repetition
@freezed
@HiveType(typeId: 3)
class ExerciseModel with _$ExerciseModel {
  const factory ExerciseModel({
    @HiveField(0) required int id,
    @HiveField(1) required int userId,
    @HiveField(2) required String subject,
    @HiveField(3) required String questionText,
    @HiveField(4) String? correctAnswer,
    @HiveField(5) String? hint,
    @HiveField(6) required DateTime nextReviewDate,
    @HiveField(7) @Default(2.5) double easeFactor, // SM-2 Algorithm
    @HiveField(8) @Default(0) int reviewCount,
    @HiveField(9) @Default(0) int repetitions,
    @HiveField(10) @Default('medium') String difficulty,
    @HiveField(11) String? lastAnswer,
    @HiveField(12) DateTime? lastReviewedAt,
    @HiveField(13) String? createdAt,
  }) = _ExerciseModel;

  factory ExerciseModel.fromJson(Map<String, dynamic> json) =>
      _$ExerciseModelFromJson(json);
}
