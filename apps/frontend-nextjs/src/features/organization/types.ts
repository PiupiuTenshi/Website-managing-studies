export interface GradeLevel {
  id: string;
  name: string;
  sortOrder: number;
}

export interface Subject {
  id: string;
  name: string;
  code: string;
}

export interface ClassRoom {
  id: string;
  name: string;
  gradeLevelId: string;
  gradeLevelName: string;
  managerId: string | null;
  managerName: string | null;
}

export interface ClassEnrollment {
  id: string;
  classRoomId: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  status: string;
}

export interface ParentStudentLink {
  id: string;
  parentId: string;
  parentName: string;
  studentId: string;
  studentName: string;
  relationship: string | null;
  status: string;
}

export interface CreateGradeLevelRequest {
  name: string;
  sortOrder: number;
}

export interface CreateSubjectRequest {
  name: string;
  code: string;
}

export interface CreateClassRoomRequest {
  name: string;
  gradeLevelId: string;
  managerId?: string | null;
}

export interface EnrollStudentRequest {
  studentId: string;
}

export interface LinkParentRequest {
  parentId: string;
  studentId: string;
  relationship?: string | null;
}
