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

          // Week View
          Expanded(
            child: calendarState.isLoading
                ? const Center(child: CircularProgressIndicator())
                : _WeekView(
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

/// Week View Widget (7-Tage-Ansicht)
class _WeekView extends StatelessWidget {
  final DateTime weekStart;
  final List<CalendarEventModel> events;

  const _WeekView({
    required this.weekStart,
    required this.events,
  });

  @override
  Widget build(BuildContext context) {
    final weekDays = List.generate(7, (index) {
      return weekStart.add(Duration(days: index));
    });

    if (events.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.event_available,
              size: 100,
              color: Colors.grey.shade300,
            ),
            const SizedBox(height: 16),
            Text(
              'Keine Termine diese Woche',
              style: TextStyle(
                fontSize: 18,
                color: Colors.grey.shade600,
              ),
            ),
          ],
        ),
      );
    }

    return ListView.builder(
      itemCount: weekDays.length,
      itemBuilder: (context, index) {
        final day = weekDays[index];
        final dayEvents = _getEventsForDay(day);

        return _DayCard(
          date: day,
          events: dayEvents,
        );
      },
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

/// Day Card (zeigt einen Tag mit seinen Events)
class _DayCard extends StatelessWidget {
  final DateTime date;
  final List<CalendarEventModel> events;

  const _DayCard({
    required this.date,
    required this.events,
  });

  @override
  Widget build(BuildContext context) {
    final isToday = _isToday(date);
    final weekdayNames = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'];

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      color: isToday ? Colors.blue.shade50 : null,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Date Header
            Row(
              children: [
                Text(
                  weekdayNames[date.weekday - 1],
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: isToday ? FontWeight.bold : FontWeight.normal,
                    color: isToday ? Colors.blue.shade700 : null,
                  ),
                ),
                const SizedBox(width: 8),
                Text(
                  DateFormat('d.M.').format(date),
                  style: TextStyle(
                    fontSize: 14,
                    color: Colors.grey.shade600,
                  ),
                ),
                if (isToday) ...[
                  const SizedBox(width: 8),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 6,
                      vertical: 2,
                    ),
                    decoration: BoxDecoration(
                      color: Colors.blue.shade700,
                      borderRadius: BorderRadius.circular(4),
                    ),
                    child: const Text(
                      'HEUTE',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 10,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ],
              ],
            ),
            const SizedBox(height: 8),

            // Events
            if (events.isEmpty)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 8),
                child: Text(
                  'Keine Termine',
                  style: TextStyle(
                    color: Colors.grey.shade500,
                    fontSize: 12,
                  ),
                ),
              )
            else
              ...events.map((event) => _EventChip(event: event)),
          ],
        ),
      ),
    );
  }

  bool _isToday(DateTime date) {
    final now = DateTime.now();
    return date.year == now.year &&
        date.month == now.month &&
        date.day == now.day;
  }
}

/// Event Chip (einzelner Event-Eintrag)
class _EventChip extends StatelessWidget {
  final CalendarEventModel event;

  const _EventChip({required this.event});

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 4),
      padding: const EdgeInsets.all(8),
      decoration: BoxDecoration(
        color: _getSourceColor(event.source),
        borderRadius: BorderRadius.circular(4),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Text(
                _formatTime(event.startTime),
                style: const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.bold,
                  fontSize: 12,
                ),
              ),
              const SizedBox(width: 4),
              Text(
                '- ${_formatTime(event.endTime)}',
                style: TextStyle(
                  color: Colors.white.withOpacity(0.8),
                  fontSize: 12,
                ),
              ),
              const Spacer(),
              if (event.location.isNotEmpty)
                Icon(
                  Icons.location_on,
                  color: Colors.white.withOpacity(0.7),
                  size: 14,
                ),
            ],
          ),
          const SizedBox(height: 4),
          Text(
            event.title,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 14,
              fontWeight: FontWeight.w500,
            ),
          ),
          if (event.location.isNotEmpty) ...[
            const SizedBox(height: 2),
            Text(
              event.location,
              style: TextStyle(
                color: Colors.white.withOpacity(0.8),
                fontSize: 11,
              ),
            ),
          ],
          if (event.professor != null && event.professor!.isNotEmpty) ...[
            const SizedBox(height: 2),
            Text(
              event.professor!,
              style: TextStyle(
                color: Colors.white.withOpacity(0.8),
                fontSize: 11,
              ),
            ),
          ],
        ],
      ),
    );
  }

  Color _getSourceColor(String source) {
    switch (source.toLowerCase()) {
      case 'rapla':
        return Colors.blue;
      case 'moodle':
        return Colors.orange;
      case 'manual':
        return Colors.green;
      default:
        return Colors.grey;
    }
  }

  String _formatTime(DateTime time) {
    return '${time.hour.toString().padLeft(2, '0')}:${time.minute.toString().padLeft(2, '0')}';
  }
}
