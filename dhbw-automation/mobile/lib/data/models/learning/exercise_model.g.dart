// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'exercise_model.dart';

// **************************************************************************
// TypeAdapterGenerator
// **************************************************************************

class ExerciseModelAdapter extends TypeAdapter<ExerciseModel> {
  @override
  final int typeId = 3;

  @override
  ExerciseModel read(BinaryReader reader) {
    final numOfFields = reader.readByte();
    final fields = <int, dynamic>{
      for (int i = 0; i < numOfFields; i++) reader.readByte(): reader.read(),
    };
    return ExerciseModel(
      id: fields[0] as int,
      userId: fields[1] as int,
      subject: fields[2] as String,
      questionText: fields[3] as String,
      correctAnswer: fields[4] as String?,
      hint: fields[5] as String?,
      nextReviewDate: fields[6] as DateTime,
      easeFactor: fields[7] as double,
      reviewCount: fields[8] as int,
      repetitions: fields[9] as int,
      difficulty: fields[10] as String,
      lastAnswer: fields[11] as String?,
      lastReviewedAt: fields[12] as DateTime?,
      createdAt: fields[13] as String?,
    );
  }

  @override
  void write(BinaryWriter writer, ExerciseModel obj) {
    writer
      ..writeByte(14)
      ..writeByte(0)
      ..write(obj.id)
      ..writeByte(1)
      ..write(obj.userId)
      ..writeByte(2)
      ..write(obj.subject)
      ..writeByte(3)
      ..write(obj.questionText)
      ..writeByte(4)
      ..write(obj.correctAnswer)
      ..writeByte(5)
      ..write(obj.hint)
      ..writeByte(6)
      ..write(obj.nextReviewDate)
      ..writeByte(7)
      ..write(obj.easeFactor)
      ..writeByte(8)
      ..write(obj.reviewCount)
      ..writeByte(9)
      ..write(obj.repetitions)
      ..writeByte(10)
      ..write(obj.difficulty)
      ..writeByte(11)
      ..write(obj.lastAnswer)
      ..writeByte(12)
      ..write(obj.lastReviewedAt)
      ..writeByte(13)
      ..write(obj.createdAt);
  }

  @override
  int get hashCode => typeId.hashCode;

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is ExerciseModelAdapter &&
          runtimeType == other.runtimeType &&
          typeId == other.typeId;
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$ExerciseModelImpl _$$ExerciseModelImplFromJson(Map<String, dynamic> json) =>
    _$ExerciseModelImpl(
      id: (json['id'] as num).toInt(),
      userId: (json['userId'] as num).toInt(),
      subject: json['subject'] as String,
      questionText: json['questionText'] as String,
      correctAnswer: json['correctAnswer'] as String?,
      hint: json['hint'] as String?,
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

Map<String, dynamic> _$$ExerciseModelImplToJson(_$ExerciseModelImpl instance) =>
    <String, dynamic>{
      'id': instance.id,
      'userId': instance.userId,
      'subject': instance.subject,
      'questionText': instance.questionText,
      'correctAnswer': instance.correctAnswer,
      'hint': instance.hint,
      'nextReviewDate': instance.nextReviewDate.toIso8601String(),
      'easeFactor': instance.easeFactor,
      'reviewCount': instance.reviewCount,
      'repetitions': instance.repetitions,
      'difficulty': instance.difficulty,
      'lastAnswer': instance.lastAnswer,
      'lastReviewedAt': instance.lastReviewedAt?.toIso8601String(),
      'createdAt': instance.createdAt,
    };
