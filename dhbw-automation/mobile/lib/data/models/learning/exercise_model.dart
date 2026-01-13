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

  factory ExerciseModel.fromJson(Map<String, dynamic> json) {
    // Handle backend compatibility: 'question' vs 'questionText'
    final questionText = json['questionText'] as String? ?? 
                        json['question'] as String? ?? 
                        '';
    
    // Handle backend compatibility: 'helpText' vs 'hint'  
    final hint = json['hint'] as String? ?? 
                 json['helpText'] as String?;
    
    return ExerciseModel(
      id: (json['id'] as num).toInt(),
      userId: (json['userId'] as num).toInt(),
      subject: json['subject'] as String,
      questionText: questionText,
      correctAnswer: json['correctAnswer'] as String?,
      hint: hint,
      nextReviewDate: DateTime.parse(json['nextReviewDate'] as String),
      easeFactor: (json['easeFactor'] as num?)?.toDouble() ?? 2.5,
      reviewCount: (json['reviewCount'] as num?)?.toInt() ?? 0,
      repetitions: (json['repetitions'] as num?)?.toInt() ?? 0,
      difficulty: json['difficulty'] as String? ?? 'medium',
      lastAnswer: json['lastAnswer'] as String?,
      lastReviewedAt: json['lastReviewedAt'] == null
          ? null
          : DateTime.parse(json['lastReviewedAt'] as String),
      createdAt: json['createdAt'] as String?,
    );
  }
}
