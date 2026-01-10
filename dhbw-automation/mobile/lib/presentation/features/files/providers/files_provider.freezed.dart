// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'files_provider.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
    'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models');

/// @nodoc
mixin _$FilesState {
  List<DocumentModel> get documents => throw _privateConstructorUsedError;
  bool get isLoading => throw _privateConstructorUsedError;
  bool get isUploading => throw _privateConstructorUsedError;
  double get uploadProgress => throw _privateConstructorUsedError;
  String? get error => throw _privateConstructorUsedError;
  String? get successMessage => throw _privateConstructorUsedError;

  /// Create a copy of FilesState
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $FilesStateCopyWith<FilesState> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $FilesStateCopyWith<$Res> {
  factory $FilesStateCopyWith(
          FilesState value, $Res Function(FilesState) then) =
      _$FilesStateCopyWithImpl<$Res, FilesState>;
  @useResult
  $Res call(
      {List<DocumentModel> documents,
      bool isLoading,
      bool isUploading,
      double uploadProgress,
      String? error,
      String? successMessage});
}

/// @nodoc
class _$FilesStateCopyWithImpl<$Res, $Val extends FilesState>
    implements $FilesStateCopyWith<$Res> {
  _$FilesStateCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of FilesState
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? documents = null,
    Object? isLoading = null,
    Object? isUploading = null,
    Object? uploadProgress = null,
    Object? error = freezed,
    Object? successMessage = freezed,
  }) {
    return _then(_value.copyWith(
      documents: null == documents
          ? _value.documents
          : documents // ignore: cast_nullable_to_non_nullable
              as List<DocumentModel>,
      isLoading: null == isLoading
          ? _value.isLoading
          : isLoading // ignore: cast_nullable_to_non_nullable
              as bool,
      isUploading: null == isUploading
          ? _value.isUploading
          : isUploading // ignore: cast_nullable_to_non_nullable
              as bool,
      uploadProgress: null == uploadProgress
          ? _value.uploadProgress
          : uploadProgress // ignore: cast_nullable_to_non_nullable
              as double,
      error: freezed == error
          ? _value.error
          : error // ignore: cast_nullable_to_non_nullable
              as String?,
      successMessage: freezed == successMessage
          ? _value.successMessage
          : successMessage // ignore: cast_nullable_to_non_nullable
              as String?,
    ) as $Val);
  }
}

/// @nodoc
abstract class _$$FilesStateImplCopyWith<$Res>
    implements $FilesStateCopyWith<$Res> {
  factory _$$FilesStateImplCopyWith(
          _$FilesStateImpl value, $Res Function(_$FilesStateImpl) then) =
      __$$FilesStateImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call(
      {List<DocumentModel> documents,
      bool isLoading,
      bool isUploading,
      double uploadProgress,
      String? error,
      String? successMessage});
}

/// @nodoc
class __$$FilesStateImplCopyWithImpl<$Res>
    extends _$FilesStateCopyWithImpl<$Res, _$FilesStateImpl>
    implements _$$FilesStateImplCopyWith<$Res> {
  __$$FilesStateImplCopyWithImpl(
      _$FilesStateImpl _value, $Res Function(_$FilesStateImpl) _then)
      : super(_value, _then);

  /// Create a copy of FilesState
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? documents = null,
    Object? isLoading = null,
    Object? isUploading = null,
    Object? uploadProgress = null,
    Object? error = freezed,
    Object? successMessage = freezed,
  }) {
    return _then(_$FilesStateImpl(
      documents: null == documents
          ? _value._documents
          : documents // ignore: cast_nullable_to_non_nullable
              as List<DocumentModel>,
      isLoading: null == isLoading
          ? _value.isLoading
          : isLoading // ignore: cast_nullable_to_non_nullable
              as bool,
      isUploading: null == isUploading
          ? _value.isUploading
          : isUploading // ignore: cast_nullable_to_non_nullable
              as bool,
      uploadProgress: null == uploadProgress
          ? _value.uploadProgress
          : uploadProgress // ignore: cast_nullable_to_non_nullable
              as double,
      error: freezed == error
          ? _value.error
          : error // ignore: cast_nullable_to_non_nullable
              as String?,
      successMessage: freezed == successMessage
          ? _value.successMessage
          : successMessage // ignore: cast_nullable_to_non_nullable
              as String?,
    ));
  }
}

/// @nodoc

class _$FilesStateImpl implements _FilesState {
  const _$FilesStateImpl(
      {final List<DocumentModel> documents = const [],
      this.isLoading = false,
      this.isUploading = false,
      this.uploadProgress = 0.0,
      this.error,
      this.successMessage})
      : _documents = documents;

  final List<DocumentModel> _documents;
  @override
  @JsonKey()
  List<DocumentModel> get documents {
    if (_documents is EqualUnmodifiableListView) return _documents;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_documents);
  }

  @override
  @JsonKey()
  final bool isLoading;
  @override
  @JsonKey()
  final bool isUploading;
  @override
  @JsonKey()
  final double uploadProgress;
  @override
  final String? error;
  @override
  final String? successMessage;

  @override
  String toString() {
    return 'FilesState(documents: $documents, isLoading: $isLoading, isUploading: $isUploading, uploadProgress: $uploadProgress, error: $error, successMessage: $successMessage)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$FilesStateImpl &&
            const DeepCollectionEquality()
                .equals(other._documents, _documents) &&
            (identical(other.isLoading, isLoading) ||
                other.isLoading == isLoading) &&
            (identical(other.isUploading, isUploading) ||
                other.isUploading == isUploading) &&
            (identical(other.uploadProgress, uploadProgress) ||
                other.uploadProgress == uploadProgress) &&
            (identical(other.error, error) || other.error == error) &&
            (identical(other.successMessage, successMessage) ||
                other.successMessage == successMessage));
  }

  @override
  int get hashCode => Object.hash(
      runtimeType,
      const DeepCollectionEquality().hash(_documents),
      isLoading,
      isUploading,
      uploadProgress,
      error,
      successMessage);

  /// Create a copy of FilesState
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$FilesStateImplCopyWith<_$FilesStateImpl> get copyWith =>
      __$$FilesStateImplCopyWithImpl<_$FilesStateImpl>(this, _$identity);
}

abstract class _FilesState implements FilesState {
  const factory _FilesState(
      {final List<DocumentModel> documents,
      final bool isLoading,
      final bool isUploading,
      final double uploadProgress,
      final String? error,
      final String? successMessage}) = _$FilesStateImpl;

  @override
  List<DocumentModel> get documents;
  @override
  bool get isLoading;
  @override
  bool get isUploading;
  @override
  double get uploadProgress;
  @override
  String? get error;
  @override
  String? get successMessage;

  /// Create a copy of FilesState
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$FilesStateImplCopyWith<_$FilesStateImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
