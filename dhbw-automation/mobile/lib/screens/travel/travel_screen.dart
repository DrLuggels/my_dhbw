import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../core/services/travel_service.dart';
import '../../core/models/travel_models.dart';

class TravelScreen extends StatefulWidget {
  const TravelScreen({super.key});

  @override
  State<TravelScreen> createState() => _TravelScreenState();
}

class _TravelScreenState extends State<TravelScreen> {
  final _fromController = TextEditingController(text: 'Laupheim West');
  final _toController = TextEditingController(text: 'Ravensburg');
  TrainConnectionResponse? _connections;
  bool _isLoading = false;
  String? _error;

  @override
  void dispose() {
    _fromController.dispose();
    _toController.dispose();
    super.dispose();
  }

  Future<void> _searchConnections() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final travelService = context.read<TravelService>();
      final request = TrainConnectionRequest(
        from: _fromController.text,
        to: _toController.text,
      );

      final connections = await travelService.getConnections(request);

      setState(() {
        _connections = connections;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _error = e.toString();
        _isLoading = false;
      });
    }
  }

  Future<void> _loadDefaultConnections() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final travelService = context.read<TravelService>();
      final connections = await travelService.getDefaultConnections();

      setState(() {
        _connections = connections;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _error = e.toString();
        _isLoading = false;
      });
    }
  }

  String _formatTime(DateTime dateTime) {
    return DateFormat('HH:mm').format(dateTime);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Zugverbindungen'),
        backgroundColor: Theme.of(context).colorScheme.primary,
        foregroundColor: Colors.white,
      ),
      body: SingleChildScrollView(
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Suchformular
              Card(
                elevation: 2,
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    children: [
                      TextField(
                        controller: _fromController,
                        decoration: const InputDecoration(
                          labelText: 'Von',
                          prefixIcon: Icon(Icons.location_on),
                          border: OutlineInputBorder(),
                        ),
                      ),
                      const SizedBox(height: 16),
                      TextField(
                        controller: _toController,
                        decoration: const InputDecoration(
                          labelText: 'Nach',
                          prefixIcon: Icon(Icons.location_on_outlined),
                          border: OutlineInputBorder(),
                        ),
                      ),
                      const SizedBox(height: 16),
                      SizedBox(
                        width: double.infinity,
                        child: ElevatedButton.icon(
                          onPressed: _isLoading ? null : _searchConnections,
                          icon: _isLoading
                              ? const SizedBox(
                                  width: 20,
                                  height: 20,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                  ),
                                )
                              : const Icon(Icons.search),
                          label: const Text('Suchen'),
                          style: ElevatedButton.styleFrom(
                            padding: const EdgeInsets.all(16),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),

              const SizedBox(height: 16),

              // Schnellzugriff Standard-Route
              OutlinedButton.icon(
                onPressed: _isLoading ? null : _loadDefaultConnections,
                icon: const Icon(Icons.bolt),
                label: const Text('Standard-Route (Laupheim West → Ravensburg)'),
                style: OutlinedButton.styleFrom(
                  padding: const EdgeInsets.all(16),
                ),
              ),

              const SizedBox(height: 16),

              // Fehleranzeige
              if (_error != null)
                Card(
                  color: Colors.red.shade50,
                  child: Padding(
                    padding: const EdgeInsets.all(16.0),
                    child: Row(
                      children: [
                        const Icon(Icons.error, color: Colors.red),
                        const SizedBox(width: 8),
                        Expanded(child: Text(_error!)),
                        IconButton(
                          icon: const Icon(Icons.close),
                          onPressed: () => setState(() => _error = null),
                        ),
                      ],
                    ),
                  ),
                ),

              // Verbindungsliste
              if (_connections != null && _connections!.journeys.isNotEmpty)
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '${_connections!.journeys.length} Verbindung(en) gefunden',
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                    const SizedBox(height: 8),
                    ..._connections!.journeys.map((journey) =>
                        _buildJourneyCard(context, journey)),
                  ],
                ),

              // Keine Ergebnisse
              if (!_isLoading &&
                  _connections != null &&
                  _connections!.journeys.isEmpty)
                const Card(
                  child: Padding(
                    padding: EdgeInsets.all(16.0),
                    child: Row(
                      children: [
                        Icon(Icons.info, color: Colors.blue),
                        SizedBox(width: 8),
                        Text('Keine Verbindungen gefunden'),
                      ],
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildJourneyCard(BuildContext context, Journey journey) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: ExpansionTile(
        title: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    _formatTime(journey.departure),
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 18,
                    ),
                  ),
                  Text(
                    journey.from,
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ],
              ),
            ),
            Column(
              children: [
                const Icon(Icons.arrow_forward),
                Text(
                  journey.duration,
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ],
            ),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(
                    _formatTime(journey.arrival),
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 18,
                    ),
                  ),
                  Text(
                    journey.to,
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ],
              ),
            ),
          ],
        ),
        subtitle: Padding(
          padding: const EdgeInsets.only(top: 8.0),
          child: Row(
            children: [
              Chip(
                label: Text(
                  '${journey.transfers} Umstieg${journey.transfers != 1 ? 'e' : ''}',
                  style: const TextStyle(fontSize: 12),
                ),
                backgroundColor: journey.transfers == 0
                    ? Colors.green.shade100
                    : Colors.blue.shade100,
              ),
              if (journey.delay != null && journey.delay! > 0) ...[
                const SizedBox(width: 8),
                Chip(
                  label: Text(
                    '+${journey.delay} min',
                    style: const TextStyle(fontSize: 12),
                  ),
                  backgroundColor: Colors.orange.shade100,
                ),
              ],
              if (journey.cancelled == true) ...[
                const SizedBox(width: 8),
                const Chip(
                  label: Text(
                    'Ausfall',
                    style: TextStyle(fontSize: 12),
                  ),
                  backgroundColor: Colors.red,
                ),
              ],
            ],
          ),
        ),
        children: journey.legs.map((leg) => _buildLegTile(context, leg)).toList(),
      ),
    );
  }

  Widget _buildLegTile(BuildContext context, Leg leg) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 8.0),
      child: Card(
        color: leg.cancelled == true
            ? Colors.red.shade50
            : Theme.of(context).colorScheme.surfaceContainerHighest,
        child: Padding(
          padding: const EdgeInsets.all(12.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.train, color: Colors.blue),
                  const SizedBox(width: 8),
                  Text(
                    leg.line ?? 'Zug',
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                  if (leg.direction != null) ...[
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        '→ ${leg.direction}',
                        style: Theme.of(context).textTheme.bodySmall,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ],
              ),
              const Divider(height: 16),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(leg.from),
                        Text(
                          'Gleis ${leg.platform ?? 'N/A'}',
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                        Text(
                          _formatTime(leg.departure),
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                  ),
                  const Icon(Icons.arrow_forward),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(leg.to),
                        const SizedBox(height: 4),
                        Text(
                          _formatTime(leg.arrival),
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              if (leg.delay != null && leg.delay! > 0)
                Padding(
                  padding: const EdgeInsets.only(top: 8.0),
                  child: Chip(
                    label: Text('Verspätung: +${leg.delay} min'),
                    backgroundColor: Colors.orange.shade100,
                    visualDensity: VisualDensity.compact,
                  ),
                ),
              if (leg.cancelled == true)
                const Padding(
                  padding: EdgeInsets.only(top: 8.0),
                  child: Chip(
                    label: Text('Ausfall'),
                    backgroundColor: Colors.red,
                    visualDensity: VisualDensity.compact,
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
