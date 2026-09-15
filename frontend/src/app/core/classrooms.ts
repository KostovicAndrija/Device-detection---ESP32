export interface Classroom {
  id: string;
  name: string;
  description: string;
  capacity: number;
  layout: 'aisle-2-3' | 'standard' | 'compact';
}

export const CLASSROOMS: Classroom[] = [
  {
    id: 'UC-101',
    name: 'Učionica 101',
    description: 'Pet redova, prolaz po sredini, 2 računara levo i 3 desno.',
    capacity: 25,
    layout: 'aisle-2-3'
  },
  {
    id: 'UC-202',
    name: 'Računarska sala 202',
    description: 'Standardna računarska učionica sa 20 mesta.',
    capacity: 20,
    layout: 'standard'
  },
  {
    id: 'LAB-A',
    name: 'Laboratorija A',
    description: 'Manja laboratorija za praktične ispite.',
    capacity: 12,
    layout: 'compact'
  }
];

export function classroomById(id: string | null | undefined): Classroom {
  return CLASSROOMS.find(room => room.id === id) ?? CLASSROOMS[0];
}
