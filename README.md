# practic_Fin

![alt text](image.png)

@startuml
' Use Case Diagram (PlantUML) для WPF-приложения
left to right direction
skinparam shadowing false
skinparam packageStyle rectangle

actor "Гость\n(неавторизованный)" as Guest
actor "Пользователь" as User
actor "Администратор" as Admin

rectangle "WPF-приложение\n«Информационная система пользователей»" as System {

  (Авторизация) as UC_Login
  (Регистрация) as UC_Register
  (Выход из учетной записи) as UC_Logout
  (Выход из приложения) as UC_Exit

  (Проверка заполненности полей) as UC_Required
  (Проверка уникальности логина) as UC_UniqueLogin
  (Проверка формата пароля) as UC_PwdFormat
  (Подтверждение пароля) as UC_PwdConfirm
  (Хеширование пароля) as UC_Hash
  (Создание учетной записи) as UC_CreateAccount

  (Просмотр пользователей) as UC_Admin_View
  (Добавление пользователя) as UC_Admin_Add
  (Редактирование пользователя) as UC_Admin_Edit
  (Удаление пользователя) as UC_Admin_Del

  (Просмотр каталога пользователей) as UC_User_View
  (Поиск по Ф.И.О.) as UC_Search
  (Фильтрация по роли) as UC_Filter
  (Сортировка по Ф.И.О.) as UC_Sort
  (Фото по умолчанию\nпри отсутствии Photo) as UC_DefaultPhoto
}

' --- Доступ гостя ---
Guest --> UC_Login
Guest --> UC_Register
Guest --> UC_Exit

' --- Общие действия для авторизованных ---
User --> UC_Login
Admin --> UC_Login
User --> UC_Logout
Admin --> UC_Logout
User --> UC_Exit
Admin --> UC_Exit

' Авторизация включает хеширование (сравнение хеша с БД)
UC_Login ..> UC_Hash : <<include>>

' Регистрация включает проверки и создание
UC_Register ..> UC_Required : <<include>>
UC_Register ..> UC_UniqueLogin : <<include>>
UC_Register ..> UC_PwdFormat : <<include>>
UC_Register ..> UC_PwdConfirm : <<include>>
UC_Register ..> UC_Hash : <<include>>
UC_Register ..> UC_CreateAccount : <<include>>

' --- Администратор ---
Admin --> UC_Admin_View
Admin --> UC_Admin_Add
Admin --> UC_Admin_Edit
Admin --> UC_Admin_Del

UC_Admin_Add ..> UC_Required : <<include>>
UC_Admin_Add ..> UC_UniqueLogin : <<include>>
UC_Admin_Add ..> UC_Hash : <<include>>

UC_Admin_Edit ..> UC_Hash : <<extend>>
note right of UC_Admin_Edit
Хеширование применяется,
если меняется пароль.
end note

UC_Admin_Del ..> UC_Required : <<extend>>
note right of UC_Admin_Del
Перед удалением требуется подтверждение.
end note

' --- Пользователь ---
User --> UC_User_View
UC_User_View ..> UC_Search : <<include>>
UC_User_View ..> UC_Filter : <<include>>
UC_User_View ..> UC_Sort : <<include>>
UC_User_View ..> UC_DefaultPhoto : <<include>>
@enduml
