"use client";

import { useEffect, useState } from "react";
import { getGradeLevels, getSubjects, getClassRooms } from "@/features/organization/api";
import type { GradeLevel, Subject, ClassRoom } from "@/features/organization/types";
import { RoleShell } from "@/features/auth/components/role-shell";

export default function AdminOrganizationPage() {
  const [grades, setGrades] = useState<GradeLevel[]>([]);
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [classes, setClasses] = useState<ClassRoom[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadData() {
      try {
        const [gData, sData, cData] = await Promise.all([
          getGradeLevels(),
          getSubjects(),
          getClassRooms()
        ]);
        setGrades(gData);
        setSubjects(sData);
        setClasses(cData);
      } catch (error) {
        console.error("Failed to load organization data:", error);
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, []);

  return (
    <RoleShell role="Admin" title="Quản lý Tổ chức Học tập">
      <div className="p-8">
        
        {loading ? (
          <p>Đang tải dữ liệu...</p>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
            {/* Grade Levels */}
            <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200">
              <h2 className="text-xl font-semibold mb-4">Khối lớp ({grades.length})</h2>
              <ul className="space-y-2">
                {grades.map(g => (
                  <li key={g.id} className="p-3 bg-gray-50 rounded-md border border-gray-100 flex justify-between">
                    <span>{g.name}</span>
                    <span className="text-sm text-gray-500">Order: {g.sortOrder}</span>
                  </li>
                ))}
                {grades.length === 0 && <p className="text-gray-500 italic">Chưa có dữ liệu</p>}
              </ul>
              <button className="mt-4 w-full py-2 bg-indigo-50 text-indigo-600 rounded hover:bg-indigo-100 transition-colors">
                + Thêm khối lớp
              </button>
            </div>

            {/* Subjects */}
            <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200">
              <h2 className="text-xl font-semibold mb-4">Môn học ({subjects.length})</h2>
              <ul className="space-y-2">
                {subjects.map(s => (
                  <li key={s.id} className="p-3 bg-gray-50 rounded-md border border-gray-100 flex justify-between">
                    <span>{s.name}</span>
                    <span className="text-xs px-2 py-1 bg-gray-200 rounded text-gray-700">{s.code}</span>
                  </li>
                ))}
                {subjects.length === 0 && <p className="text-gray-500 italic">Chưa có dữ liệu</p>}
              </ul>
              <button className="mt-4 w-full py-2 bg-indigo-50 text-indigo-600 rounded hover:bg-indigo-100 transition-colors">
                + Thêm môn học
              </button>
            </div>

            {/* Classes */}
            <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200">
              <h2 className="text-xl font-semibold mb-4">Lớp học ({classes.length})</h2>
              <ul className="space-y-2">
                {classes.map(c => (
                  <li key={c.id} className="p-3 bg-gray-50 rounded-md border border-gray-100 flex justify-between">
                    <span>{c.name}</span>
                    <span className="text-sm text-gray-500">{c.gradeLevelName}</span>
                  </li>
                ))}
                {classes.length === 0 && <p className="text-gray-500 italic">Chưa có dữ liệu</p>}
              </ul>
              <button className="mt-4 w-full py-2 bg-indigo-50 text-indigo-600 rounded hover:bg-indigo-100 transition-colors">
                + Thêm lớp học
              </button>
            </div>
          </div>
        )}
      </div>
    </RoleShell>
  );
}
