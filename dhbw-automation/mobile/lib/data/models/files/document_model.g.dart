// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'document_model.dart';

// **************************************************************************
// TypeAdapterGenerator
// **************************************************************************

class DocumentModelAdapter extends TypeAdapter<DocumentModel> {
  @override
  final int typeId = 1;

  @override
  DocumentModel read(BinaryReader reader) {
    final numOfFields = reader.readByte();
    final fields = <int, dynamic>{
      for (int i = 0; i < numOfFields; i++) reader.readByte(): reader.read(),
    };
    return DocumentModel(
      id: fields[0] as int,
      userId: fields[1] as int,
      fileName: fields[2] as String,
      filePath: fields[3] as String,
      fileType: fields[4] as String,
      fileSize: fields[5] as int,
      category: fields[6] as String,
      isProcessed: fields[7] as bool,
      summary: fields[8] as String?,
      tags: fields[9] as String?,
      extractedText: fields[10] as String?,
      uploadedAt: fields[11] as String?,
      processedAt: fields[12] as String?,
    );
  }

  @override
  void write(BinaryWriter writer, DocumentModel obj) {
    writer
      ..writeByte(13)
      ..writeByte(0)
      ..write(obj.id)
      ..writeByte(1)
      ..write(obj.userId)
      ..writeByte(2)
      ..write(obj.fileName)
      ..writeByte(3)
      ..write(obj.filePath)
      ..writeByte(4)
      ..write(obj.fileType)
      ..writeByte(5)
      ..write(obj.fileSize)
      ..writeByte(6)
      ..write(obj.category)
      ..writeByte(7)
      ..write(obj.isProcessed)
      ..writeByte(8)
      ..write(obj.summary)
      ..writeByte(9)
      ..write(obj.tags)
      ..writeByte(10)
      ..write(obj.extractedText)
      ..writeByte(11)
      ..write(obj.uploadedAt)
      ..writeByte(12)
      ..write(obj.processedAt);
  }

  @override
  int get hashCode => typeId.hashCode;

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is DocumentModelAdapter &&
          runtimeType == other.runtimeType &&
          typeId == other.typeId;
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$DocumentModelImpl _$$DocumentModelImplFromJson(Map<String, dynamic> json) =>
    _$DocumentModelImpl(
      id: (json['id'] as num).toInt(),
      userId: (json['userId'] as num).toInt(),
      fileName: json['fileName'] as String,
      filePath: json['filePath'] as String,
      fileType: json['fileType'] as String,
      fileSize: (json['fileSize'] as num).toInt(),
      category: json['category'] as String,
      isProcessed: json['isProcessed'] as bool? ?? false,
      summary: json['summary'] as String?,
      tags: json['tags'] as String?,
      extractedText: json['extractedText'] as String?,
      uploadedAt: json['uploadedAt'] as String?,
      processedAt: json['processedAt'] as String?,
    );

Map<String, dynamic> _$$DocumentModelImplToJson(_$DocumentModelImpl instance) =>
    <String, dynamic>{
      'id': instance.id,
      'userId': instance.userId,
      'fileName': instance.fileName,
      'filePath': instance.filePath,
      'fileType': instance.fileType,
      'fileSize': instance.fileSize,
      'category': instance.category,
      'isProcessed': instance.isProcessed,
      'summary': instance.summary,
      'tags': instance.tags,
      'extractedText': instance.extractedText,
      'uploadedAt': instance.uploadedAt,
      'processedAt': instance.processedAt,
    };
