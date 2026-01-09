import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'core/services/storage_service.dart';
import 'core/services/api_client.dart';
import 'core/services/auth_service.dart';
import 'core/services/calendar_service.dart';
import 'core/services/todo_service.dart';
import 'core/services/mail_service.dart';
import 'core/services/file_service.dart';
import 'providers/auth_provider.dart';
import 'screens/auth/login_screen.dart';
import 'screens/auth/register_screen.dart';
import 'screens/home_screen.dart';

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    // Initialize services
    final storageService = StorageService();
    final apiClient = ApiClient(storageService);
    final authService = AuthService(apiClient, storageService);
    final calendarService = CalendarService(apiClient);
    final todoService = TodoService(apiClient);
    final mailService = MailService(apiClient);
    final fileService = FileService(apiClient);

    return MultiProvider(
      providers: [
        Provider<StorageService>.value(value: storageService),
        Provider<ApiClient>.value(value: apiClient),
        Provider<AuthService>.value(value: authService),
        Provider<CalendarService>.value(value: calendarService),
        Provider<TodoService>.value(value: todoService),
        Provider<MailService>.value(value: mailService),
        Provider<FileService>.value(value: fileService),
        ChangeNotifierProvider(
          create: (_) => AuthProvider(authService),
        ),
      ],
      child: MaterialApp(
        title: 'DHBW Automation',
        theme: ThemeData(
          colorScheme: ColorScheme.fromSeed(seedColor: Colors.blue),
          useMaterial3: true,
          appBarTheme: const AppBarTheme(
            centerTitle: true,
            elevation: 0,
          ),
        ),
        initialRoute: '/',
        routes: {
          '/': (context) => const SplashScreen(),
          '/login': (context) => const LoginScreen(),
          '/register': (context) => const RegisterScreen(),
          '/home': (context) => const HomeScreen(),
        },
      ),
    );
  }
}

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> {
  @override
  void initState() {
    super.initState();
    _checkAuth();
  }

  Future<void> _checkAuth() async {
    final authService = context.read<AuthService>();
    final isLoggedIn = await authService.isLoggedIn();
    
    if (mounted) {
      Navigator.pushReplacementNamed(
        context,
        isLoggedIn ? '/home' : '/login',
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      body: Center(
        child: CircularProgressIndicator(),
      ),
    );
  }
}
