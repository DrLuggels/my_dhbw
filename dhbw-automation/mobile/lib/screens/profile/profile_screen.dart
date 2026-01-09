import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/auth_provider.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Profil'),
      ),
      body: ListView(
        children: [
          const SizedBox(height: 20),
          const CircleAvatar(
            radius: 50,
            backgroundColor: Colors.blue,
            child: Icon(Icons.person, size: 60, color: Colors.white),
          ),
          const SizedBox(height: 16),
          const Text(
            'Student',
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: 24,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 8),
          const Text(
            'student@dhbw.de',
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: 16,
              color: Colors.grey,
            ),
          ),
          const SizedBox(height: 32),
          const Divider(),
          ListTile(
            leading: const Icon(Icons.settings),
            title: const Text('Einstellungen'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () {
              // TODO: Navigate to settings
            },
          ),
          ListTile(
            leading: const Icon(Icons.notifications),
            title: const Text('Benachrichtigungen'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () {
              // TODO: Navigate to notification settings
            },
          ),
          ListTile(
            leading: const Icon(Icons.sync),
            title: const Text('Synchronisation'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () {
              // TODO: Navigate to sync settings
            },
          ),
          const Divider(),
          ListTile(
            leading: const Icon(Icons.info_outline),
            title: const Text('Über die App'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () {
              showAboutDialog(
                context: context,
                applicationName: 'DHBW Automation',
                applicationVersion: '1.0.0',
                applicationLegalese: '© 2026 DHBW Automation',
              );
            },
          ),
          ListTile(
            leading: const Icon(Icons.help_outline),
            title: const Text('Hilfe & Support'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () {
              // TODO: Navigate to help
            },
          ),
          const Divider(),
          ListTile(
            leading: const Icon(Icons.logout, color: Colors.red),
            title: const Text(
              'Abmelden',
              style: TextStyle(color: Colors.red),
            ),
            onTap: () async {
              final confirmed = await showDialog<bool>(
                context: context,
                builder: (context) => AlertDialog(
                  title: const Text('Abmelden'),
                  content: const Text('Möchtest du dich wirklich abmelden?'),
                  actions: [
                    TextButton(
                      onPressed: () => Navigator.pop(context, false),
                      child: const Text('Abbrechen'),
                    ),
                    ElevatedButton(
                      onPressed: () => Navigator.pop(context, true),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.red,
                      ),
                      child: const Text('Abmelden'),
                    ),
                  ],
                ),
              );

              if (confirmed == true && context.mounted) {
                await context.read<AuthProvider>().logout();
                if (context.mounted) {
                  Navigator.pushNamedAndRemoveUntil(
                    context,
                    '/login',
                    (route) => false,
                  );
                }
              }
            },
          ),
        ],
      ),
    );
  }
}
