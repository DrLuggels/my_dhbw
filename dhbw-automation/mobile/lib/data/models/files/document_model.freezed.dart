// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'document_model.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
    'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models');

DocumentModel _$DocumentModelFromJson(Map<String, dynamic> json) {
  return _DocumentModel.fromJson(json);
}

/// @nodoc
mixin _$DocumentModel {
  @HiveField(0)
  int get id => throw _privateConstructorUsedError;
  @HiveField(1)
  int get userId => throw _privateConstructorUsedError;
  @HiveField(2)
  String get fileName => throw _privateConstructorUsedError;
  @HiveField(3)
  String get filePath => throw _privateConstructorUsedError;
  @HiveField(4)
  String get fileType => throw _privateConstructorUsedError;
  @HiveField(5)
  int get fileSize => throw _privateConstructorUsedError;
  @HiveField(6)
  String get category => throw _privateConstructorUsedError;
  @HiveField(7)
  bool get isProcessed => throw _privateConstructorUsedError;
  @HiveField(8)
  String? get summary => throw _privateConstructorUsedError;
  @HiveField(9)
  String? get tags => throw _privateConstructorUsedError;
  @HiveField(10)
  String? get extractedText => throw _privateConstructorUsedError;
  @HiveField(11)
  String? get uploadedAt => throw _privateConstructorUsedError;
  @HiveField(12)
  String? get processedAt => throw _privateConstructorUsedError;

  /// Serializes this DocumentModel to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of DocumentModel
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $DocumentModelCopyWith<DocumentModel> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $DocumentModelCopyWith<$Res> {
  factory $DocumentModelCopyWith(
          DocumentModel value, $Res Function(DocumentModel) then) =
      _$DocumentModelCopyWithImpl<$Res, DocumentModel>;
  @useResult
  $Res call(
      {@HiveField(0) int id,
      @HiveField(1) int userId,
      @HiveField(2) String fileName,
      @HiveField(3) String filePath,
      @HiveField(4) String fileType,
      @HiveField(5) int fileSize,
      @HiveField(6) String category,
      @HiveField(7) bool isProcessed,
      @HiveField(8) String? summary,
      @HiveField(9) String? tags,
      @HiveField(10) String? extractedText,
      @HiveField(11) String? uploadedAt,
      @HiveField(12) String? processedAt});
}

/// @nodoc
class _$DocumentModelCopyWithImpl<$Res, $Val extends DocumentModel>
    implements $DocumentModelCopyWith<$Res> {
  _$DocumentModelCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of DocumentModel
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? userId = null,
    Object? fileName = null,
    Object? filePath = null,
    Object? fileType = null,
    Object? fileSize = null,
    Object? category = null,
    Object? isProcessed = null,
    Object? summary = freezed,
    Object? tags = freezed,
    Object? extractedText = freezed,
    Object? uploadedAt = freezed,
    Object? processedAt = freezed,
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
      fileName: null == fileName
          ? _value.fileName
          : fileName // ignore: cast_nullable_to_non_nullable
              as String,
      filePath: null == filePath
          ? _value.filePath
          : filePath // ignore: cast_nullable_to_non_nullable
              as String,
      fileType: null == fileType
          ? _value.fileType
          : fileType // ignore: cast_nullable_to_non_nullable
              as String,
      fileSize: null == fileSize
          ? _value.fileSize
          : fileSize // ignore: cast_nullable_to_non_nullable
              as int,
      category: null == category
          ? _value.category
          : category // ignore: cast_nullable_to_non_nullable
              as String,
      isProcessed: null == isProcessed
          ? _value.isProcessed
          : isProcessed // ignore: cast_nullable_to_non_nullable
              as bool,
      summary: freezed == summary
          ? _value.summary
          : summary // ignore: cast_nullable_to_non_nullable
              as String?,
      tags: freezed == tags
          ? _value.tags
          : tags // ignore: cast_nullable_to_non_nullable
              as String?,
      extractedText: freezed == extractedText
          ? _value.extractedText
          : extractedText // ignore: cast_nullable_to_non_nullable
              as String?,
      uploadedAt: freezed == uploadedAt
          ? _value.uploadedAt
          : uploadedAt // ignore: cast_nullable_to_non_nullable
              as String?,
      processedAt: freezed == processedAt
          ? _value.processedAt
          : processedAt // ignore: cast_nullable_to_non_nullable
              as String?,
    ) as $Val);
  }
}

/// @nodoc
abstract class _$$DocumentModelImplCopyWith<$Res>
    implements $DocumentModelCopyWith<$Res> {
  factory _$$DocumentModelImplCopyWith(
          _$DocumentModelImpl value, $Res Function(_$DocumentModelImpl) then) =
      __$$DocumentModelImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call(
      {@HiveField(0) int id,
      @HiveField(1) int userId,
      @HiveField(2) String fileName,
      @HiveField(3) String filePath,
      @HiveField(4) String fileType,
      @HiveField(5) int fileSize,
      @HiveField(6) String category,
      @HiveField(7) bool isProcessed,
      @HiveField(8) String? summary,
      @HiveField(9) String? tags,
      @HiveField(10) String? extractedText,
      @HiveField(11) String? uploadedAt,
      @HiveField(12) String? processedAt});
}

/// @nodoc
class __$$DocumentModelImplCopyWithImpl<$Res>
    extends _$DocumentModelCopyWithImpl<$Res, _$DocumentModelImpl>
    implements _$$DocumentModelImplCopyWith<$Res> {
  __$$DocumentModelImplCopyWithImpl(
      _$DocumentModelImpl _value, $Res Function(_$DocumentModelImpl) _then)
      : super(_value, _then);

  /// Create a copy of DocumentModel
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? userId = null,
    Object? fileName = null,
    Object? filePath = null,
    Object? fileType = null,
    Object? fileSize = null,
    Object? category = null,
    Object? isProcessed = null,
    Object? summary = freezed,
    Object? tags = freezed,
    Object? extractedText = freezed,
    Object? uploadedAt = freezed,
    Object? processedAt = freezed,
  }) {
    return _then(_$DocumentModelImpl(
      id: null == id
          ? _value.id
          : id // ignore: cast_nullable_to_non_nullable
              as int,
      userId: null == userId
          ? _value.userId
          : userId // ignore: cast_nullable_to_non_nullable
              as int,
      fileName: null == fileName
          ? _value.fileName
          : fileName // ignore: cast_nullable_to_non_nullable
              as String,
      filePath: null == filePath
          ? _value.filePath
          : filePath // ignore: cast_nullable_to_non_nullable
              as String,
      fileType: null == fileType
          ? _value.fileType
          : fileType // ignore: cast_nullable_to_non_nullable
              as String,
      fileSize: null == fileSize
          ? _value.fileSize
          : fileSize // ignore: cast_nullable_to_non_nullable
              as int,
      category: null == category
          ? _value.category
          : category // ignore: cast_nullable_to_non_nullable
              as String,
      isProcessed: null == isProcessed
          ? _value.isProcessed
          : isProcessed // ignore: cast_nullable_to_non_nullable
              as bool,
      summary: freezed == summary
          ? _value.summary
          : summary // ignore: cast_nullable_to_non_nullable
              as String?,
      tags: freezed == tags
          ? _value.tags
          : tags // ignore: cast_nullable_to_non_nullable
              as String?,
      extractedText: freezed == extractedText
          ? _value.extractedText
          : extractedText // ignore: cast_nullable_to_non_nullable
              as String?,
      uploadedAt: freezed == uploadedAt
          ? _value.uploadedAt
          : uploadedAt // ignore: cast_nullable_to_non_nullable
              as String?,
      processedAt: freezed == processedAt
          ? _value.processedAt
          : processedAt // ignore: cast_nullable_to_non_nullable
              as String?,
    ));
  }
}

/// @nodoc
@JsonSerializable()
class _$DocumentModelImpl implements _DocumentModel {
  const _$DocumentModelImpl(
      {@HiveField(0) required this.id,
      @HiveField(1) required this.userId,
      @HiveField(2) required this.fileName,
      @HiveField(3) required this.filePath,
      @HiveField(4) required this.fileType,
      @HiveField(5) required this.fileSize,
      @HiveField(6) required this.category,
      @HiveField(7) this.isProcessed = false,
      @HiveField(8) this.summary,
      @HiveField(9) this.tags,
      @HiveField(10) this.extractedText,
      @HiveField(11) this.uploadedAt,
      @HiveField(12) this.processedAt});

  factory _$DocumentModelImpl.fromJson(Map<String, dynamic> json) =>
      _$$DocumentModelImplFromJson(json);

  @override
  @HiveField(0)
  final int id;
  @override
  @HiveField(1)
  final int userId;
  @override
  @HiveField(2)
  final String fileName;
  @override
  @HiveField(3)
  final String filePath;
  @override
  @HiveField(4)
  final String fileType;
  @override
  @HiveField(5)
  final int fileSize;
  @override
  @HiveField(6)
  final String category;
  @override
  @JsonKey()
  @HiveField(7)
  final bool isProcessed;
  @override
  @HiveField(8)
  final String? summary;
  @override
  @HiveField(9)
  final String? tags;
  @override
  @HiveField(10)
  final String? extractedText;
  @override
  @HiveField(11)
  final String? uploadedAt;
  @override
  @HiveField(12)
  final String? processedAt;

  @override
  String toString() {
    return 'DocumentModel(id: $id, userId: $userId, fileName: $fileName, filePath: $filePath, fileType: $fileType, fileSize: $fileSize, category: $category, isProcessed: $isProcessed, summary: $summary, tags: $tags, extractedText: $extractedText, uploadedAt: $uploadedAt, processedAt: $processedAt)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$DocumentModelImpl &&
            (identical(other.id, id) || other.id == id) &&
            (identical(other.userId, userId) || other.userId == userId) &&
            (identical(other.fileName, fileName) ||
                other.fileName == fileName) &&
            (identical(other.filePath, filePath) ||
                other.filePath == filePath) &&
            (identical(other.fileType, fileType) ||
                other.fileType == fileType) &&
            (identical(other.fileSize, fileSize) ||
                other.fileSize == fileSize) &&
            (identical(other.category, category) ||
                other.category == category) &&
            (identical(other.isProcessed, isProcessed) ||
                other.isProcessed == isProcessed) &&
            (identical(other.summary, summary) || other.summary == summary) &&
            (identical(other.tags, tags) || other.tags == tags) &&
            (identical(other.extractedText, extractedText) ||
                other.extractedText == extractedText) &&
            (identical(other.uploadedAt, uploadedAt) ||
                other.uploadedAt == uploadedAt) &&
            (identical(other.processedAt, processedAt) ||
                other.processedAt == processedAt));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
      runtimeType,
      id,
      userId,
      fileName,
      filePath,
      fileType,
      fileSize,
      category,
      isProcessed,
      summary,
      tags,
      extractedText,
      uploadedAt,
      processedAt);

  /// Create a copy of DocumentModel
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$DocumentModelImplCopyWith<_$DocumentModelImpl> get copyWith =>
      __$$DocumentModelImplCopyWithImpl<_$DocumentModelImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$DocumentModelImplToJson(
      this,
    );
  }
}

abstract class _DocumentModel implements DocumentModel {
  const factory _DocumentModel(
      {@HiveField(0) required final int id,
      @HiveField(1) required final int userId,
      @HiveField(2) required final String fileName,
      @HiveField(3) required final String filePath,
      @HiveField(4) required final String fileType,
      @HiveField(5) required final int fileSize,
      @HiveField(6) required final String category,
      @HiveField(7) final bool isProcessed,
      @HiveField(8) final String? summary,
      @HiveField(9) final String? tags,
      @HiveField(10) final String? extractedText,
      @HiveField(11) final String? uploadedAt,
      @HiveField(12) final String? processedAt}) = _$DocumentModelImpl;

  factory _DocumentModel.fromJson(Map<String, dynamic> json) =
      _$DocumentModelImpl.fromJson;

  @override
  @HiveField(0)
  int get id;
  @override
  @HiveField(1)
  int get userId;
  @override
  @HiveField(2)
  String get fileName;
  @override
  @HiveField(3)
  String get filePath;
  @override
  @HiveField(4)
  String get fileType;
  @override
  @HiveField(5)
  int get fileSize;
  @override
  @HiveField(6)
  String get category;
  @override
  @HiveField(7)
  bool get isProcessed;
  @override
  @HiveField(8)
  String? get summary;
  @override
  @HiveField(9)
  String? get tags;
  @override
  @HiveField(10)
  String? get extractedText;
  @override
  @HiveField(11)
  String? get uploadedAt;
  @override
  @HiveField(12)
  String? get processedAt;

  /// Create a copy of DocumentModel
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$DocumentModelImplCopyWith<_$DocumentModelImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
