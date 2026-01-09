import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:table_calendar/table_calendar.dart';
import 'package:intl/intl.dart';
import 'package:pull_to_refresh/pull_to_refresh.dart';
import '../../core/models/calendar_event.dart';
import '../../core/services/calendar_service.dart';
import 'dart:async';

class CalendarScreen extends StatefulWidget {
  const CalendarScreen({super.key});

  @override
  State<CalendarScreen> createState() => _CalendarScreenState();
}

class _CalendarScreenState extends State<CalendarScreen> {
  DateTime _focusedDay = DateTime.now();
  DateTime? _selectedDay;
  List<CalendarEvent> _events = [];
  bool _isLoading = false;
  Timer? _pollTimer;
  final RefreshController _refreshController = RefreshController();

  @override
  void initState() {
    super.initState();
    _selectedDay = _focusedDay;
    _loadEvents();
    _startPolling();
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    _refreshController.dispose();
    super.dispose();
  }

  void _startPolling() {
    _pollTimer = Timer.periodic(const Duration(seconds: 30), (_) {
      _loadEvents(silent: true);
    });
  }

  Future<void> _loadEvents({bool silent = false}) async {
    if (!silent) {
      setState(() => _isLoading = true);
    }

    final calendarService = context.read<CalendarService>();
    
    final startOfWeek = _focusedDay.subtract(Duration(days: _focusedDay.weekday - 1));
    final endOfWeek = startOfWeek.add(const Duration(days: 6));

    final events = await calendarService.getEvents(
      startDate: startOfWeek,
      endDate: endOfWeek,
    );

    setState(() {
      _events = events;
      _isLoading = false;
    });

    _refreshController.refreshCompleted();
  }

  List<CalendarEvent> _getEventsForDay(DateTime day) {
    return _events.where((event) {
      return event.startTime.year == day.year &&
          event.startTime.month == day.month &&
          event.startTime.day == day.day;
    }).toList();
  }

  Future<void> _syncRapla() async {
    try {
      final calendarService = context.read<CalendarService>();
      await calendarService.syncRapla();
      
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Rapla synchronisiert')),
        );
        _loadEvents();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Fehler: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final selectedDayEvents = _selectedDay != null
        ? _getEventsForDay(_selectedDay!)
        : <CalendarEvent>[];

    return Scaffold(
      appBar: AppBar(
        title: const Text('Kalender'),
        actions: [
          IconButton(
            icon: const Icon(Icons.sync),
            onPressed: _syncRapla,
            tooltip: 'Rapla synchronisieren',
          ),
        ],
      ),
      body: SmartRefresher(
        controller: _refreshController,
        onRefresh: () => _loadEvents(),
        child: _isLoading && _events.isEmpty
            ? const Center(child: CircularProgressIndicator())
            : Column(
                children: [
                  TableCalendar(
                    firstDay: DateTime.now().subtract(const Duration(days: 365)),
                    lastDay: DateTime.now().add(const Duration(days: 365)),
                    focusedDay: _focusedDay,
                    selectedDayPredicate: (day) => isSameDay(_selectedDay, day),
                    eventLoader: _getEventsForDay,
                    calendarFormat: CalendarFormat.week,
                    startingDayOfWeek: StartingDayOfWeek.monday,
                    headerStyle: const HeaderStyle(
                      formatButtonVisible: false,
                      titleCentered: true,
                    ),
                    onDaySelected: (selectedDay, focusedDay) {
                      setState(() {
                        _selectedDay = selectedDay;
                        _focusedDay = focusedDay;
                      });
                    },
                    onPageChanged: (focusedDay) {
                      _focusedDay = focusedDay;
                      _loadEvents();
                    },
                  ),
                  const Divider(height: 1),
                  Expanded(
                    child: selectedDayEvents.isEmpty
                        ? Center(
                            child: Text(
                              'Keine Termine für ${DateFormat('dd.MM.yyyy').format(_selectedDay!)}',
                              style: const TextStyle(color: Colors.grey),
                            ),
                          )
                        : ListView.builder(
                            itemCount: selectedDayEvents.length,
                            itemBuilder: (context, index) {
                              final event = selectedDayEvents[index];
                              return _EventTile(event: event);
                            },
                          ),
                  ),
                ],
              ),
      ),
    );
  }
}

class _EventTile extends StatelessWidget {
  final CalendarEvent event;

  const _EventTile({required this.event});

  Color _getSourceColor(String source) {
    switch (source.toLowerCase()) {
      case 'rapla':
        return Colors.blue;
      case 'google':
        return Colors.green;
      case 'manual':
        return Colors.orange;
      default:
        return Colors.grey;
    }
  }

  @override
  Widget build(BuildContext context) {
    final timeFormat = DateFormat('HH:mm');
    
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      child: ListTile(
        leading: Container(
          width: 4,
          height: double.infinity,
          color: _getSourceColor(event.source),
        ),
        title: Text(
          event.title,
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 4),
            Row(
              children: [
                const Icon(Icons.access_time, size: 16, color: Colors.grey),
                const SizedBox(width: 4),
                Text(
                  '${timeFormat.format(event.startTime)} - ${timeFormat.format(event.endTime)}',
                  style: const TextStyle(fontSize: 13),
                ),
              ],
            ),
            if (event.location != null) ...[
              const SizedBox(height: 4),
              Row(
                children: [
                  const Icon(Icons.location_on, size: 16, color: Colors.grey),
                  const SizedBox(width: 4),
                  Text(event.location!, style: const TextStyle(fontSize: 13)),
                ],
              ),
            ],
            if (event.notes != null) ...[
              const SizedBox(height: 4),
              Text(
                event.notes!,
                style: const TextStyle(fontSize: 12, fontStyle: FontStyle.italic),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ],
        ),
        trailing: Chip(
          label: Text(
            event.source.toUpperCase(),
            style: const TextStyle(fontSize: 10),
          ),
          backgroundColor: _getSourceColor(event.source).withOpacity(0.2),
        ),
        onTap: () {
          // TODO: Show event details/edit notes
        },
      ),
    );
  }
}
