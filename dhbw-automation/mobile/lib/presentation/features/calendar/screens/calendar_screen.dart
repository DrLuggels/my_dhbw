import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../providers/calendar_provider.dart';
import '../../../../data/models/calendar/calendar_event_model.dart';

class CalendarScreen extends ConsumerWidget {
  const CalendarScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final calendarState = ref.watch(calendarProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text('KW ${calendarState.weekNumber}'),
        actions: [
          if (calendarState.isSyncing)
            const Padding(
              padding: EdgeInsets.all(16.0),
              child: SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
            )
          else
            IconButton(
              icon: const Icon(Icons.sync),
              onPressed: () => ref.read(calendarProvider.notifier).syncRapla(),
              tooltip: 'Rapla synchronisieren',
            ),
          IconButton(
            icon: const Icon(Icons.today),
            onPressed: () => ref.read(calendarProvider.notifier).goToToday(),
            tooltip: 'Heute',
          ),
        ],
      ),
      body: Column(
        children: [
          // Week Navigation
          Container(
            padding: const EdgeInsets.symmetric(vertical: 8),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                IconButton(
                  icon: const Icon(Icons.chevron_left),
                  onPressed: calendarState.isLoading
                      ? null
                      : () => ref.read(calendarProvider.notifier).previousWeek(),
                ),
                Text(
                  _formatWeekRange(
                    calendarState.weekStart,
                    calendarState.weekEnd,
                  ),
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w500,
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.chevron_right),
                  onPressed: calendarState.isLoading
                      ? null
                      : () => ref.read(calendarProvider.notifier).nextWeek(),
                ),
              ],
            ),
          ),

          // Success/Error Messages
          if (calendarState.successMessage != null)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              color: Colors.green.shade50,
              child: Row(
                children: [
                  Icon(Icons.check_circle, color: Colors.green.shade700),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      calendarState.successMessage!,
                      style: TextStyle(color: Colors.green.shade700),
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close, size: 20),
                    onPressed: () =>
                        ref.read(calendarProvider.notifier).clearMessages(),
                  ),
                ],
              ),
            ),

          if (calendarState.error != null)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              color: Colors.red.shade50,
              child: Row(
                children: [
                  Icon(Icons.error_outline, color: Colors.red.shade700),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      calendarState.error!,
                      style: TextStyle(color: Colors.red.shade700),
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close, size: 20),
                    onPressed: () =>
                        ref.read(calendarProvider.notifier).clearMessages(),
                  ),
                ],
              ),
            ),

          // Week Grid View
          Expanded(
            child: calendarState.isLoading
                ? const Center(child: CircularProgressIndicator())
                : _WeekGridView(
                    weekStart: calendarState.weekStart,
                    events: calendarState.weekEvents,
                  ),
          ),
        ],
      ),
    );
  }

  String _formatWeekRange(DateTime start, DateTime end) {
    final format = DateFormat('d. MMM', 'de');
    return '${format.format(start)} - ${format.format(end.subtract(const Duration(days: 1)))}';
  }
}

/// Week Grid View - Shows 7 days with time slots
class _WeekGridView extends StatelessWidget {
  final DateTime weekStart;
  final List<CalendarEventModel> events;

  // Time range configuration
  static const int startHour = 7;
  static const int endHour = 20;
  static const double hourHeight = 60.0;
  static const double timeColumnWidth = 45.0;

  const _WeekGridView({
    required this.weekStart,
    required this.events,
  });

  @override
  Widget build(BuildContext context) {
    final weekDays = List.generate(7, (index) {
      return weekStart.add(Duration(days: index));
    });

    return Column(
      children: [
        // Day Headers (Mo, Di, Mi, ...)
        _buildDayHeaders(weekDays),

        // Scrollable Time Grid
        Expanded(
          child: SingleChildScrollView(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Time Column
                _buildTimeColumn(),

                // Day Columns with Events
                Expanded(
                  child: Row(
                    children: weekDays.map((day) {
                      return Expanded(
                        child: _DayColumn(
                          date: day,
                          events: _getEventsForDay(day),
                          startHour: startHour,
                          endHour: endHour,
                          hourHeight: hourHeight,
                        ),
                      );
                    }).toList(),
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildDayHeaders(List<DateTime> weekDays) {
    final weekdayNames = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'];
    final now = DateTime.now();

    return Container(
      decoration: BoxDecoration(
        border: Border(bottom: BorderSide(color: Colors.grey.shade300)),
      ),
      child: Row(
        children: [
          // Empty space for time column
          SizedBox(width: timeColumnWidth),
          // Day headers
          ...weekDays.asMap().entries.map((entry) {
            final index = entry.key;
            final day = entry.value;
            final isToday = day.year == now.year &&
                day.month == now.month &&
                day.day == now.day;

            return Expanded(
              child: Container(
                padding: const EdgeInsets.symmetric(vertical: 8),
                decoration: BoxDecoration(
                  color: isToday ? Colors.blue.shade50 : null,
                  border: Border(
                    left: index > 0
                        ? BorderSide(color: Colors.grey.shade200)
                        : BorderSide.none,
                  ),
                ),
                child: Column(
                  children: [
                    Text(
                      weekdayNames[index],
                      style: TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 12,
                        color: isToday ? Colors.blue.shade700 : Colors.grey.shade700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                      decoration: isToday
                          ? BoxDecoration(
                              color: Colors.blue.shade700,
                              borderRadius: BorderRadius.circular(12),
                            )
                          : null,
                      child: Text(
                        '${day.day}',
                        style: TextStyle(
                          fontSize: 14,
                          fontWeight: isToday ? FontWeight.bold : FontWeight.normal,
                          color: isToday ? Colors.white : Colors.grey.shade600,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            );
          }),
        ],
      ),
    );
  }

  Widget _buildTimeColumn() {
    return SizedBox(
      width: timeColumnWidth,
      child: Column(
        children: List.generate(endHour - startHour, (index) {
          final hour = startHour + index;
          return SizedBox(
            height: hourHeight,
            child: Align(
              alignment: Alignment.topRight,
              child: Padding(
                padding: const EdgeInsets.only(right: 4, top: 0),
                child: Text(
                  '$hour:00',
                  style: TextStyle(
                    fontSize: 10,
                    color: Colors.grey.shade600,
                  ),
                ),
              ),
            ),
          );
        }),
      ),
    );
  }

  List<CalendarEventModel> _getEventsForDay(DateTime date) {
    return events.where((event) {
      return event.startTime.year == date.year &&
          event.startTime.month == date.month &&
          event.startTime.day == date.day;
    }).toList()
      ..sort((a, b) => a.startTime.compareTo(b.startTime));
  }
}

/// Single Day Column with time grid and events
class _DayColumn extends StatelessWidget {
  final DateTime date;
  final List<CalendarEventModel> events;
  final int startHour;
  final int endHour;
  final double hourHeight;

  const _DayColumn({
    required this.date,
    required this.events,
    required this.startHour,
    required this.endHour,
    required this.hourHeight,
  });

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final isToday = date.year == now.year &&
        date.month == now.month &&
        date.day == now.day;
    final totalHeight = (endHour - startHour) * hourHeight;

    return Container(
      height: totalHeight,
      decoration: BoxDecoration(
        color: isToday ? Colors.blue.shade50.withOpacity(0.3) : null,
        border: Border(
          left: BorderSide(color: Colors.grey.shade200),
        ),
      ),
      child: Stack(
        children: [
          // Hour grid lines
          ...List.generate(endHour - startHour, (index) {
            return Positioned(
              top: index * hourHeight,
              left: 0,
              right: 0,
              child: Container(
                height: 1,
                color: Colors.grey.shade200,
              ),
            );
          }),

          // Events
          ...events.map((event) => _buildEventBlock(context, event)),

          // Current time indicator (red line)
          if (isToday) _buildCurrentTimeIndicator(now),
        ],
      ),
    );
  }

  Widget _buildEventBlock(BuildContext context, CalendarEventModel event) {
    final startMinutes = event.startTime.hour * 60 + event.startTime.minute;
    final endMinutes = event.endTime.hour * 60 + event.endTime.minute;
    final gridStartMinutes = startHour * 60;

    final top = (startMinutes - gridStartMinutes) / 60 * hourHeight;
    final height = (endMinutes - startMinutes) / 60 * hourHeight;

    // Clamp to visible area
    final clampedTop = top.clamp(0.0, (endHour - startHour) * hourHeight);
    final clampedHeight = height.clamp(20.0, (endHour - startHour) * hourHeight - clampedTop);

    return Positioned(
      top: clampedTop,
      left: 1,
      right: 1,
      height: clampedHeight,
      child: GestureDetector(
        onTap: () => _showEventDetails(context, event),
        child: Container(
          margin: const EdgeInsets.only(bottom: 1),
          padding: const EdgeInsets.all(2),
          decoration: BoxDecoration(
            color: _getSourceColor(event.source),
            borderRadius: BorderRadius.circular(4),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.1),
                blurRadius: 2,
                offset: const Offset(0, 1),
              ),
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                _formatTime(event.startTime),
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 9,
                  fontWeight: FontWeight.bold,
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              if (clampedHeight > 30)
                Expanded(
                  child: Text(
                    event.title,
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 10,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildCurrentTimeIndicator(DateTime now) {
    final currentMinutes = now.hour * 60 + now.minute;
    final gridStartMinutes = startHour * 60;
    final top = (currentMinutes - gridStartMinutes) / 60 * hourHeight;

    if (top < 0 || top > (endHour - startHour) * hourHeight) {
      return const SizedBox.shrink();
    }

    return Positioned(
      top: top,
      left: 0,
      right: 0,
      child: Row(
        children: [
          Container(
            width: 8,
            height: 8,
            decoration: const BoxDecoration(
              color: Colors.red,
              shape: BoxShape.circle,
            ),
          ),
          Expanded(
            child: Container(
              height: 2,
              color: Colors.red,
            ),
          ),
        ],
      ),
    );
  }

  void _showEventDetails(BuildContext context, CalendarEventModel event) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (context) => DraggableScrollableSheet(
        initialChildSize: 0.6,
        minChildSize: 0.3,
        maxChildSize: 0.9,
        expand: false,
        builder: (context, scrollController) => SingleChildScrollView(
          controller: scrollController,
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Handle bar
                Center(
                  child: Container(
                    width: 40,
                    height: 4,
                    margin: const EdgeInsets.only(bottom: 20),
                    decoration: BoxDecoration(
                      color: Colors.grey.shade300,
                      borderRadius: BorderRadius.circular(2),
                    ),
                  ),
                ),

                // Event Title
                Text(
                  event.title,
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 16),

                // Source Badge
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                  decoration: BoxDecoration(
                    color: _getSourceColor(event.source),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Text(
                    event.source.toUpperCase(),
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                const SizedBox(height: 20),

                // Time
                _buildDetailRow(
                  Icons.access_time,
                  'Zeit',
                  '${_formatTime(event.startTime)} - ${_formatTime(event.endTime)}',
                ),

                // Date
                _buildDetailRow(
                  Icons.calendar_today,
                  'Datum',
                  _formatDate(event.startTime),
                ),

                // Duration
                _buildDetailRow(
                  Icons.timelapse,
                  'Dauer',
                  _formatDuration(event.startTime, event.endTime),
                ),

                // Location
                if (event.location.isNotEmpty)
                  _buildDetailRow(
                    Icons.location_on,
                    'Ort',
                    event.location,
                  ),

                // Subject
                if (event.subject.isNotEmpty)
                  _buildDetailRow(
                    Icons.school,
                    'Fach',
                    event.subject,
                  ),

                // Professor
                if (event.professor != null && event.professor!.isNotEmpty)
                  _buildDetailRow(
                    Icons.person,
                    'Dozent',
                    event.professor!,
                  ),

                // Event Type
                if (event.eventType != null && event.eventType!.isNotEmpty)
                  _buildDetailRow(
                    Icons.category,
                    'Typ',
                    event.eventType!,
                  ),

                // Description
                if (event.description != null && event.description!.isNotEmpty) ...[
                  const SizedBox(height: 16),
                  Text(
                    'Beschreibung',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: Colors.grey.shade600,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    event.description!,
                    style: const TextStyle(fontSize: 14),
                  ),
                ],

                // Notes
                if (event.notes != null && event.notes!.isNotEmpty) ...[
                  const SizedBox(height: 16),
                  Text(
                    'Notizen',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: Colors.grey.shade600,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: Colors.yellow.shade50,
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: Colors.yellow.shade200),
                    ),
                    child: Text(
                      event.notes!,
                      style: const TextStyle(fontSize: 14),
                    ),
                  ),
                ],

                const SizedBox(height: 24),

                // Close Button
                SizedBox(
                  width: double.infinity,
                  child: ElevatedButton(
                    onPressed: () => Navigator.pop(context),
                    child: const Text('Schließen'),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildDetailRow(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 20, color: Colors.grey.shade600),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.grey.shade600,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  value,
                  style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Color _getSourceColor(String source) {
    switch (source.toLowerCase()) {
      case 'rapla':
        return Colors.blue.shade600;
      case 'moodle':
        return Colors.orange.shade600;
      case 'manual':
        return Colors.green.shade600;
      default:
        return Colors.grey.shade600;
    }
  }

  String _formatTime(DateTime time) {
    return '${time.hour.toString().padLeft(2, '0')}:${time.minute.toString().padLeft(2, '0')}';
  }

  String _formatDate(DateTime date) {
    final weekdays = ['Montag', 'Dienstag', 'Mittwoch', 'Donnerstag', 'Freitag', 'Samstag', 'Sonntag'];
    final months = ['Januar', 'Februar', 'März', 'April', 'Mai', 'Juni', 'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'];
    return '${weekdays[date.weekday - 1]}, ${date.day}. ${months[date.month - 1]} ${date.year}';
  }

  String _formatDuration(DateTime start, DateTime end) {
    final duration = end.difference(start);
    final hours = duration.inHours;
    final minutes = duration.inMinutes % 60;

    if (hours > 0 && minutes > 0) {
      return '$hours Std. $minutes Min.';
    } else if (hours > 0) {
      return '$hours Stunde${hours > 1 ? 'n' : ''}';
    } else {
      return '$minutes Minuten';
    }
  }
}
