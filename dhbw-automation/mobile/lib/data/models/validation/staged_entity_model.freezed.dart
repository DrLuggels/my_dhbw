// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'staged_entity_model.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
    'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models');

StagedEntityModel _$StagedEntityModelFromJson(Map<String, dynamic> json) {
  return _StagedEntityModel.fromJson(json);
}

/// @nodoc
mixin _$StagedEntityModel {
  int get id => throw _privateConstructorUsedError;
  int get userId => throw _privateConstructorUsedError;
  String get entityType =>
      throw _privateConstructorUsedError; // 'todo', 'meeting', 'project', 'learning_deficit', 'reminder'
  String get entityData => throw _privateConstructorUsedError; // JSON string
  int get confidenceScore => throw _privateConstructorUsedError; // 0-100
  String get status =>
      throw _privateConstructorUsedError; // 'pending_review', 'confirmed', 'modified', 'rejected'
  String get priority =>
      throw _privateConstructorUsedError; // 'low', 'medium', 'high', 'urgent'
  bool get isPromoted => throw _privateConstructorUsedError;
  int? get promotedEntityId => throw _privateConstructorUsedError;
  int? get sourceDocumentId => throw _privateConstructorUsedError;
  List<AIQuestionModel> get questions => throw _privateConstructorUsedError;
  String? get createdAt => throw _privateConstructorUsedError;
  String? get reviewedAt => throw _privateConstructorUsedError;

  /// Serializes this StagedEntityModel to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of StagedEntityModel
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $StagedEntityModelCopyWith<StagedEntityModel> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $StagedEntityModelCopyWith<$Res> {
  factory $StagedEntityModelCopyWith(
          StagedEntityModel value, $Res Function(StagedEntityModel) then) =
      _$StagedEntityModelCopyWithImpl<$Res, StagedEntityModel>;
  @useResult
  $Res call(
      {int id,
      int userId,
      String entityType,
      String entityData,
      int confidenceScore,
      String status,
      String priority,
      bool isPromoted,
      int? promotedEntityId,
      int? sourceDocumentId,
      List<AIQuestionModel> questions,
      String? createdAt,
      String? reviewedAt});
}

/// @nodoc
class _$StagedEntityModelCopyWithImpl<$Res, $Val extends StagedEntityModel>
    implements $StagedEntityModelCopyWith<$Res> {
  _$StagedEntityModelCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of StagedEntityModel
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? userId = null,
    Object? entityType = null,
    Object? entityData = null,
    Object? confidenceScore = null,
    Object? status = null,
    Object? priority = null,
    Object? isPromoted = null,
    Object? promotedEntityId = freezed,
    Object? sourceDocumentId = freezed,
    Object? questions = null,
    Object? createdAt = freezed,
    Object? reviewedAt = freezed,
  }) {
    return _then(_value.copyWith(
      id: null == id
          ? _value.id
          : id // ignore: cast_nullable_to_non_nullable
              as int,
      userId: null == userId
          ? _value.userId
          : userId // ignore: cast_nullable_to_non_nullable
              as int,
      entityType: null == entityType
          ? _value.entityType
          : entityType // ignore: cast_nullable_to_non_nullable
              as String,
      entityData: null == entityData
          ? _value.entityData
          : entityData // ignore: cast_nullable_to_non_nullable
              as String,
      confidenceScore: null == confidenceScore
          ? _value.confidenceScore
          : confidenceScore // ignore: cast_nullable_to_non_nullable
              as int,
      status: null == status
          ? _value.status
          : status // ignore: cast_nullable_to_non_nullable
              as String,
      priority: null == priority
          ? _value.priority
          : priority // ignore: cast_nullable_to_non_nullable
              as String,
      isPromoted: null == isPromoted
          ? _value.isPromoted
          : isPromoted // ignore: cast_nullable_to_non_nullable
              as bool,
      promotedEntityId: freezed == promotedEntityId
          ? _value.promotedEntityId
          : promotedEntityId // ignore: cast_nullable_to_non_nullable
              as int?,
      sourceDocumentId: freezed == sourceDocumentId
          ? _value.sourceDocumentId
          : sourceDocumentId // ignore: cast_nullable_to_non_nullable
              as int?,
      questions: null == questions
          ? _value.questions
          : questions // ignore: cast_nullable_to_non_nullable
              as List<AIQuestionModel>,
      createdAt: freezed == createdAt
          ? _value.createdAt
          : createdAt // ignore: cast_nullable_to_non_nullable
              as String?,
      reviewedAt: freezed == reviewedAt
          ? _value.reviewedAt
          : reviewedAt // ignore: cast_nullable_to_non_nullable
              as String?,
    ) as $Val);
  }
}

/// @nodoc
abstract class _$$StagedEntityModelImplCopyWith<$Res>
    implements $StagedEntityModelCopyWith<$Res> {
  factory _$$StagedEntityModelImplCopyWith(_$StagedEntityModelImpl value,
          $Res Function(_$StagedEntityModelImpl) then) =
      __$$StagedEntityModelImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call(
      {int id,
      int userId,
      String entityType,
      String entityData,
      int confidenceScore,
      String status,
      String priority,
      bool isPromoted,
      int? promotedEntityId,
      int? sourceDocumentId,
      List<AIQuestionModel> questions,
      String? createdAt,
      String? reviewedAt});
}

/// @nodoc
class __$$StagedEntityModelImplCopyWithImpl<$Res>
    extends _$StagedEntityModelCopyWithImpl<$Res, _$StagedEntityModelImpl>
    implements _$$StagedEntityModelImplCopyWith<$Res> {
  __$$StagedEntityModelImplCopyWithImpl(_$StagedEntityModelImpl _value,
      $Res Function(_$StagedEntityModelImpl) _then)
      : super(_value, _then);

  /// Create a copy of StagedEntityModel
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? userId = null,
    Object? entityType = null,
    Object? entityData = null,
    Object? confidenceScore = null,
    Object? status = null,
    Object? priority = null,
    Object? isPromoted = null,
    Object? promotedEntityId = freezed,
    Object? sourceDocumentId = freezed,
    Object? questions = null,
    Object? createdAt = freezed,
    Object? reviewedAt = freezed,
  }) {
    return _then(_$StagedEntityModelImpl(
      id: null == id
          ? _value.id
          : id // ignore: cast_nullable_to_non_nullable
              as int,
      userId: null == userId
          ? _value.userId
          : userId // ignore: cast_nullable_to_non_nullable
              as int,
      entityType: null == entityType
          ? _value.entityType
          : entityType // ignore: cast_nullable_to_non_nullable
              as String,
      entityData: null == entityData
          ? _value.entityData
          : entityData // ignore: cast_nullable_to_non_nullable
              as String,
      confidenceScore: null == confidenceScore
          ? _value.confidenceScore
          : confidenceScore // ignore: cast_nullable_to_non_nullable
              as int,
      status: null == status
          ? _value.status
          : status // ignore: cast_nullable_to_non_nullable
              as String,
      priority: null == priority
          ? _value.priority
          : priority // ignore: cast_nullable_to_non_nullable
              as String,
      isPromoted: null == isPromoted
          ? _value.isPromoted
          : isPromoted // ignore: cast_nullable_to_non_nullable
              as bool,
      promotedEntityId: freezed == promotedEntityId
          ? _value.promotedEntityId
          : promotedEntityId // ignore: cast_nullable_to_non_nullable
              as int?,
      sourceDocumentId: freezed == sourceDocumentId
          ? _value.sourceDocumentId
          : sourceDocumentId // ignore: cast_nullable_to_non_nullable
              as int?,
      questions: null == questions
          ? _value._questions
          : questions // ignore: cast_nullable_to_non_nullable
              as List<AIQuestionModel>,
      createdAt: freezed == createdAt
          ? _value.createdAt
          : createdAt // ignore: cast_nullable_to_non_nullable
              as String?,
      reviewedAt: freezed == reviewedAt
          ? _value.reviewedAt
          : reviewedAt // ignore: cast_nullable_to_non_nullable
              as String?,
    ));
  }
}

/// @nodoc
@JsonSerializable()
class _$StagedEntityModelImpl implements _StagedEntityModel {
  const _$StagedEntityModelImpl(
      {required this.id,
      required this.userId,
      required this.entityType,
      required this.entityData,
      required this.confidenceScore,
      required this.status,
      required this.priority,
      required this.isPromoted,
      this.promotedEntityId,
      this.sourceDocumentId,
      final List<AIQuestionModel> questions = const [],
      this.createdAt,
      this.reviewedAt})
      : _questions = questions;

  factory _$StagedEntityModelImpl.fromJson(Map<String, dynamic> json) =>
      _$$StagedEntityModelImplFromJson(json);

  @override
  final int id;
  @override
  final int userId;
  @override
  final String entityType;
// 'todo', 'meeting', 'project', 'learning_deficit', 'reminder'
  @override
  final String entityData;
// JSON string
  @override
  final int confidenceScore;
// 0-100
  @override
  final String status;
// 'pending_review', 'confirmed', 'modified', 'rejected'
  @override
  final String priority;
// 'low', 'medium', 'high', 'urgent'
  @override
  final bool isPromoted;
  @override
  final int? promotedEntityId;
  @override
  final int? sourceDocumentId;
  final List<AIQuestionModel> _questions;
  @override
  @JsonKey()
  List<AIQuestionModel> get questions {
    if (_questions is EqualUnmodifiableListView) return _questions;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_questions);
  }

  @override
  final String? createdAt;
  @override
  final String? reviewedAt;

  @override
  String toString() {
    return 'StagedEntityModel(id: $id, userId: $userId, entityType: $entityType, entityData: $entityData, confidenceScore: $confidenceScore, status: $status, priority: $priority, isPromoted: $isPromoted, promotedEntityId: $promotedEntityId, sourceDocumentId: $sourceDocumentId, questions: $questions, createdAt: $createdAt, reviewedAt: $reviewedAt)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$StagedEntityModelImpl &&
            (identical(other.id, id) || other.id == id) &&
            (identical(other.userId, userId) || other.userId == userId) &&
            (identical(other.entityType, entityType) ||
                other.entityType == entityType) &&
            (identical(other.entityData, entityData) ||
                other.entityData == entityData) &&
            (identical(other.confidenceScore, confidenceScore) ||
                other.confidenceScore == confidenceScore) &&
            (identical(other.status, status) || other.status == status) &&
            (identical(other.priority, priority) ||
                other.priority == priority) &&
            (identical(other.isPromoted, isPromoted) ||
                other.isPromoted == isPromoted) &&
            (identical(other.promotedEntityId, promotedEntityId) ||
                other.promotedEntityId == promotedEntityId) &&
            (identical(other.sourceDocumentId, sourceDocumentId) ||
                other.sourceDocumentId == sourceDocumentId) &&
            const DeepCollectionEquality()
                .equals(other._questions, _questions) &&
            (identical(other.createdAt, createdAt) ||
                other.createdAt == createdAt) &&
            (identical(other.reviewedAt, reviewedAt) ||
                other.reviewedAt == reviewedAt));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
      runtimeType,
      id,
      userId,
      entityType,
      entityData,
      confidenceScore,
      status,
      priority,
      isPromoted,
      promotedEntityId,
      sourceDocumentId,
      const DeepCollectionEquality().hash(_questions),
      createdAt,
      reviewedAt);

  /// Create a copy of StagedEntityModel
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$StagedEntityModelImplCopyWith<_$StagedEntityModelImpl> get copyWith =>
      __$$StagedEntityModelImplCopyWithImpl<_$StagedEntityModelImpl>(
          this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$StagedEntityModelImplToJson(
      this,
    );
  }
}

abstract class _StagedEntityModel implements StagedEntityModel {
  const factory _StagedEntityModel(
      {required final int id,
      required final int userId,
      required final String entityType,
      required final String entityData,
      required final int confidenceScore,
      required final String status,
      required final String priority,
      required final bool isPromoted,
      final int? promotedEntityId,
      final int? sourceDocumentId,
      final List<AIQuestionModel> questions,
      final String? createdAt,
      final String? reviewedAt}) = _$StagedEntityModelImpl;

  factory _StagedEntityModel.fromJson(Map<String, dynamic> json) =
      _$StagedEntityModelImpl.fromJson;

  @override
  int get id;
  @override
  int get userId;
  @override
  String
      get entityType; // 'todo', 'meeting', 'project', 'learning_deficit', 'reminder'
  @override
  String get entityData; // JSON string
  @override
  int get confidenceScore; // 0-100
  @override
  String get status; // 'pending_review', 'confirmed', 'modified', 'rejected'
  @override
  String get priority; // 'low', 'medium', 'high', 'urgent'
  @override
  bool get isPromoted;
  @override
  int? get promotedEntityId;
  @override
  int? get sourceDocumentId;
  @override
  List<AIQuestionModel> get questions;
  @override
  String? get createdAt;
  @override
  String? get reviewedAt;

  /// Create a copy of StagedEntityModel
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$StagedEntityModelImplCopyWith<_$StagedEntityModelImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
