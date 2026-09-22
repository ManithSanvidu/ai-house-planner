import React, { useState } from 'react';
import type { CalendarEventDto } from '../../../services/constructorWorkflowService';
import { ChevronLeft, ChevronRight } from 'lucide-react';

interface ConstructorProjectCalendarProps {
  events: CalendarEventDto[];
  onEventClick: (logId: string) => void;
  onDateClick: (dateStr: string) => void;
  isReadOnly: boolean;
}

export const ConstructorProjectCalendar: React.FC<ConstructorProjectCalendarProps> = ({ 
  events, 
  onEventClick, 
  onDateClick, 
  isReadOnly 
}) => {
  const [currentDate, setCurrentDate] = useState(new Date());

  const getDaysInMonth = (year: number, month: number) => {
    return new Date(year, month + 1, 0).getDate();
  };

  const getFirstDayOfMonth = (year: number, month: number) => {
    return new Date(year, month, 1).getDay();
  };

  const prevMonth = () => {
    setCurrentDate(new Date(currentDate.getFullYear(), currentDate.getMonth() - 1, 1));
  };

  const nextMonth = () => {
    setCurrentDate(new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 1));
  };
  
  const goToToday = () => {
    setCurrentDate(new Date());
  };

  const year = currentDate.getFullYear();
  const month = currentDate.getMonth();
  const daysInMonth = getDaysInMonth(year, month);
  const firstDay = getFirstDayOfMonth(year, month);
  
  const today = new Date();
  const isToday = (d: number) => today.getDate() === d && today.getMonth() === month && today.getFullYear() === year;

  // Format YYYY-MM-DD
  const formatDate = (d: number) => {
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${year}-${pad(month + 1)}-${pad(d)}`;
  };

  const monthNames = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
  const dayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

  const getEventsForDay = (d: number) => {
    const dateStr = formatDate(d);
    return events.filter(e => e.date.startsWith(dateStr));
  };

  const renderCells = () => {
    const cells = [];
    
    // Empty cells for days before the 1st
    for (let i = 0; i < firstDay; i++) {
      cells.push(<div key={`empty-${i}`} className="min-h-[100px] border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/30"></div>);
    }
    
    // Days of the month
    for (let d = 1; d <= daysInMonth; d++) {
      const dateStr = formatDate(d);
      const dayEvents = getEventsForDay(d);
      
      cells.push(
        <div 
          key={d} 
          className={`min-h-[100px] border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-1 flex flex-col ${!isReadOnly ? 'cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700' : ''}`}
          onClick={(e) => {
             // Only trigger date click if we clicked the cell itself, not an event
             if (e.target === e.currentTarget && !isReadOnly) {
               onDateClick(dateStr);
             }
          }}
        >
          <div className="flex justify-between items-center mb-1 px-1">
            <span className={`text-sm font-medium ${isToday(d) ? 'bg-indigo-600 text-white rounded-full w-6 h-6 flex items-center justify-center' : 'text-gray-700 dark:text-gray-300'}`}>
              {d}
            </span>
            {!isReadOnly && (
               <span 
                 className="text-xs text-gray-400 hover:text-indigo-600 cursor-pointer hidden md:block opacity-0 group-hover:opacity-100 transition-opacity"
                 onClick={() => onDateClick(dateStr)}
               >
                 +
               </span>
            )}
          </div>
          
          <div className="flex-1 space-y-1 overflow-y-auto custom-scrollbar">
            {dayEvents.map(ev => (
              <div 
                key={ev.id} 
                onClick={(e) => {
                  e.stopPropagation();
                  if (ev.dailyLogId) onEventClick(ev.dailyLogId);
                }}
                className={`text-[10px] leading-tight p-1 rounded cursor-pointer truncate ${ev.color === '#ef4444' ? 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-200' : ev.color === '#8b5cf6' ? 'bg-purple-100 text-purple-800 dark:bg-purple-900/40 dark:text-purple-200' : 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-200'}`}
                title={ev.description}
              >
                <span className="font-semibold block truncate">{ev.title}</span>
                <span className="truncate opacity-90">{ev.description}</span>
              </div>
            ))}
          </div>
        </div>
      );
    }
    
    // Fill remaining cells for grid balance
    const totalCells = cells.length;
    const remaining = 42 - totalCells; // 6 rows of 7
    if (remaining > 0 && remaining < 7) {
       for (let i = 0; i < remaining; i++) {
         cells.push(<div key={`end-empty-${i}`} className="min-h-[100px] border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/30"></div>);
       }
    }
    
    return cells;
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-xl p-4 md:p-6 shadow-sm ring-1 ring-gray-900/5 dark:ring-white/10">
      
      <div className="flex flex-col sm:flex-row justify-between items-center mb-6 gap-4">
        <div className="flex items-center gap-4">
           <button onClick={prevMonth} className="p-1 rounded-full hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-600 dark:text-gray-300">
             <ChevronLeft className="w-5 h-5" />
           </button>
           <h2 className="text-xl font-bold text-gray-900 dark:text-white w-40 text-center">
             {monthNames[month]} {year}
           </h2>
           <button onClick={nextMonth} className="p-1 rounded-full hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-600 dark:text-gray-300">
             <ChevronRight className="w-5 h-5" />
           </button>
           
           <button onClick={goToToday} className="px-3 py-1 text-sm border border-gray-300 rounded-md hover:bg-gray-50 dark:border-gray-600 dark:hover:bg-gray-700 dark:text-gray-200 ml-2">
             Today
           </button>
        </div>
        
        <div className="flex flex-wrap gap-3 text-xs font-medium text-gray-600 dark:text-gray-300">
          <div className="flex items-center"><span className="w-3 h-3 rounded-full bg-blue-500 mr-1.5"></span> Work Log</div>
          <div className="flex items-center"><span className="w-3 h-3 rounded-full bg-red-500 mr-1.5"></span> Issue / Delay</div>
          <div className="flex items-center"><span className="w-3 h-3 rounded-full bg-purple-500 mr-1.5"></span> Planned</div>
        </div>
      </div>
      
      <div className="grid grid-cols-7 gap-0">
        {dayNames.map(day => (
          <div key={day} className="text-center font-semibold text-xs py-2 text-gray-500 uppercase tracking-wider border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50">
            {day}
          </div>
        ))}
        {renderCells()}
      </div>
    </div>
  );
};

export default ConstructorProjectCalendar;
