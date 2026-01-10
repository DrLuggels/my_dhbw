// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'calendar_event_model.dart';

// **************************************************************************
// TypeAdapterGenerator
// **************************************************************************

class CalendarEventModelAdapter extends TypeAdapter<CalendarEventModel> {
  @override
  final int typeId = 2;

  @override
  CalendarEventModel read(BinaryReader reader) {
    final numOfFields = reader.readByte();
    final fields = <int, dynamic>{
      for (int i = 0; i < numOfFields; i++) reader.readByte(): reader.read(),
    };
    return CalendarEventModel(
      id: fields[0] as int,
      userId: fields[1] as int,
      title: fields[2] as String,
      startTime: fields[3] as DateTime,
      endTime: fields[4] as DateTime,
      location: fields[5] as String,
      subject: fields[6] as String,
      source: fields[7] as String,
      eventType: fields[8] as String?,
      description: fields[9] as String?,
      professor: fields[10] as String?,
      notes: fields[11] as String?,
      createdAt: fields[12] as String?,
    );
  }

  @override
  void write(BinaryWriter writer, CalendarEventModel obj) {
    writer
      ..writeByte(13)
      ..writeByte(0)
      ..write(obj.id)
      ..writeByte(1)
      ..write(obj.userId)
      ..writeByte(2)
      ..write(obj.title)
      ..writeByte(3)
      ..write(obj.startTime)
      ..writeByte(4)
      ..write(obj.endTime)
      ..writeByte(5)
      ..write(obj.location)
      ..writeByte(6)
      ..write(obj.subject)
      ..writeByte(7)
      ..write(obj.source)
      ..writeByte(8)
      ..write(obj.eventType)
      ..writeByte(9)
      ..write(obj.description)
      ..writeByte(10)
      ..write(obj.professor)
      ..writeByte(11)
      ..write(obj.notes)
      ..writeByte(12)
      ..write(obj.createdAt);
  }

  @override
  int get hashCode => typeId.hashCode;

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is CalendarEventModelAdapter &&
          runtimeType == other.runtimeType &&
          typeId == other.typeId;
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$CalendarEventModelImpl _$$CalendarEventModelImplFromJson(
        Map<String, dynamic> json) =>
    _$CalendarEventModelImpl(
      id: (json['id'] as num).toInt(),
      userId: (json['userId'] as num).toInt(),
      title: json['title'] as String,
      startTime: DateTime.parse(json['startTime'] as String),
      endTime: DateTime.parse(json['endTime'] as String),
      location: json['location'] as String,
      subject: json['subject'] as String,
      source: json['source'] as String,
      eventType: json['eventType'] as String?,
      description: json['description'] as String?,
      professor: json['professor'] as String?,
      notes: json['notes'] as String?,
      createdAt: json['createdAt'] as String?,
    );

Map<String, dynamic> _$$CalendarEventModelImplToJson(
        _$CalendarEventModelImpl instance) =>
    <String, dynamic>{
      'id': instance.id,
      'userId': instance.userId,
      'title': instance.title,
      'startTime': instance.startTime.toIso8601String(),
      'endTime': instance.endTime.toIso8601String(),
      'location': instance.location,
      'subject': instance.subject,
      'source': instance.source,
      'eventType': instance.eventType,
      'description': instance.description,
      'professor': instance.professor,
      'notes': instance.notes,
      'createdAt': instance.createdAt,
    };
