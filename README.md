# Student Management System

A full-stack application for managing students, courses, course registrations, and attendance tracking.

## Tech Stack

### Backend (.NET 8 Web API)
- .NET 8
- Entity Framework Core
- MySQL (with Pomelo.EntityFrameworkCore.MySql)
- ASP.NET Core Web API
- Swagger/OpenAPI

### Frontend (React TypeScript)
- React 18
- TypeScript
- Vite
- React Router
- Axios

## Features

### Backend API Endpoints

#### Students
- `GET /api/students` - Get all students
- `GET /api/students/{id}` - Get student by ID
- `POST /api/students` - Create new student
- `PUT /api/students/{id}` - Update student
- `DELETE /api/students/{id}` - Delete student

#### Courses
- `GET /api/courses` - Get all courses
- `GET /api/courses/{id}` - Get course by ID
- `POST /api/courses` - Create new course
- `PUT /api/courses/{id}` - Update course
- `DELETE /api/courses/{id}` - Delete course

#### Course Registrations
- `GET /api/courseregistrations` - Get all registrations
- `GET /api/courseregistrations/{id}` - Get registration by ID
- `GET /api/courseregistrations/student/{studentId}` - Get student's registrations
- `GET /api/courseregistrations/course/{courseId}` - Get course registrations
- `POST /api/courseregistrations` - Create new registration
- `DELETE /api/courseregistrations/{id}` - Delete registration

#### Attendance
- `GET /api/attendance` - Get all attendance records
- `GET /api/attendance/{id}` - Get attendance by ID
- `GET /api/attendance/student/{studentId}` - Get student's attendance
- `POST /api/attendance` - Create attendance record
- `PUT /api/attendance/{id}` - Update attendance record
- `DELETE /api/attendance/{id}` - Delete attendance record
- `POST /api/attendance/clockin` - Clock in student
- `POST /api/attendance/clockout` - Clock out student

### Frontend Features

#### Student Management
- Student registration form
- Student list with edit/delete functionality
- View student details

#### Course Management
- Course registration form
- Course list with edit/delete functionality
- View course details

#### Course Assignment
- Assign students to courses
- View current assignments
- Unassign students from courses

#### Attendance Management
- Clock in/out students
- View today's attendance
- Real-time attendance status

#### Attendance Records
- View all attendance records
- Filter by student and date
- Calculate duration between clock in/out
- Delete attendance records

## Database Schema

### Students Table
- `Id` (GUID, Primary Key)
- `FirstName` (VARCHAR(100), Required)
- `LastName` (VARCHAR(100), Required)
- `Email` (VARCHAR(255), Required, Unique)
- `PhoneNumber` (VARCHAR(20))
- `Gender` (VARCHAR(10), Required)
- `CreatedAt` (DATETIME, Default: UTC_TIMESTAMP())

### Courses Table
- `Id` (GUID, Primary Key)
- `CourseCode` (VARCHAR(20), Required, Unique)
- `CourseTitle` (VARCHAR(200), Required)
- `CreatedAt` (DATETIME, Default: UTC_TIMESTAMP())

### CourseRegistrations Table
- `Id` (GUID, Primary Key)
- `StudentId` (GUID, Foreign Key, Required)
- `CourseId` (GUID, Foreign Key, Required)
- `CreatedAt` (DATETIME, Default: UTC_TIMESTAMP())
- Unique constraint on (StudentId, CourseId)

### Attendances Table
- `Id` (GUID, Primary Key)
- `StudentId` (GUID, Foreign Key, Required)
- `ClockIn` (DATETIME, Nullable)
- `ClockOut` (DATETIME, Nullable)
- `CreatedAt` (DATETIME, Default: UTC_TIMESTAMP())

## Setup Instructions

### Prerequisites
- .NET 8 SDK
- Node.js (v16 or higher)
- MySQL Server
- Git

### Backend Setup

1. **Navigate to the API directory:**
   ```bash
   cd StudentManagementApi
   ```

2. **Install dependencies:**
   ```bash
   dotnet restore
   ```

3. **Configure MySQL connection:**
   Update `appsettings.json` with your MySQL connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=StudentManagementDb;Uid=root;Pwd=your_password;"
     }
   }
   ```

4. **Create and run database migrations:**
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

5. **Run the API:**
   ```bash
   dotnet run
   ```

   The API will be available at `https://localhost:5001` or `http://localhost:5000`

### Frontend Setup

1. **Navigate to the frontend directory:**
   ```bash
   cd student-management-frontend
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Start the development server:**
   ```bash
   npm run dev
   ```

   The frontend will be available at `http://localhost:3000`

### Database Setup

1. **Create MySQL database:**
   ```sql
   CREATE DATABASE StudentManagementDb;
   ```

2. **Create a MySQL user (optional):**
   ```sql
   CREATE USER 'student_admin'@'localhost' IDENTIFIED BY 'password';
   GRANT ALL PRIVILEGES ON StudentManagementDb.* TO 'student_admin'@'localhost';
   FLUSH PRIVILEGES;
   ```

## Usage

1. **Start the backend API** (runs on port 5000/5001)
2. **Start the frontend** (runs on port 3000)
3. **Access the application** at `http://localhost:3000`

### Workflow
1. **Register Students** - Add students to the system
2. **Register Courses** - Add courses to the system
3. **Assign Courses** - Assign students to courses
4. **Take Attendance** - Clock students in and out
5. **View Records** - Monitor attendance records and statistics

## API Documentation

When the backend is running, visit `https://localhost:5001/swagger` to view the interactive API documentation.

## Development

### Backend Development
- The API follows RESTful conventions
- Entity Framework Core handles database operations
- CORS is configured to allow requests from the React frontend
- Swagger/OpenAPI provides interactive documentation

### Frontend Development
- React with TypeScript for type safety
- Vite for fast development and building
- React Router for navigation
- Axios for API communication
- Responsive design with CSS Grid and Flexbox

## Error Handling

- Backend returns structured error responses
- Frontend displays user-friendly error messages
- Validation on both client and server sides
- Unique constraints prevent duplicate registrations

## Future Enhancements

- User authentication and authorization
- Role-based access control
- Email notifications
- Report generation
- Mobile app
- Real-time updates with SignalR
- Bulk operations
- Data export/import
- Advanced filtering and search