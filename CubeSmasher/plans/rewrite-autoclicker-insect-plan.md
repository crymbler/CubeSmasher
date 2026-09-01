# План: AutoClicker — добавить спавн Insect (визуальный эффект)

## Цель
После начисления `AddMoney(ClickPerSecond)` спавнить Insect в случайной позиции на экране, который летит ко рту лягушки. Только визуал, деньги уже начислены.

## Изменения только в одном файле

### `AutoClicker.cs`

**Добавить поля:**
- `[SerializeField] private Insect _insectPrefab` — префаб насекомого
- `[SerializeField] private Transform _target` — куда лететь (лягушка)
- `[SerializeField] private Camera _camera` — для конвертации Screen→World

**В `MakeClick()` — после `_wallet.AddMoney(...)`:**
- Сгенерировать случайную позицию на экране (x: 0..Screen.width, y: 0..Screen.height)
- Сконвертировать в мировые координаты через `_camera.ScreenToWorldPoint`
- Z = transform.position.z (как у спавнера)
- Заспавнить Insect: `Insect insect = Instantiate(_insectPrefab, spawnPos, Quaternion.identity)`
- Вызвать `insect.Move(_target)`

**Всё.** Остальные файлы не трогаем.

## Порядок реализации
1. Добавить поля в AutoClicker.cs
2. Дописать спавн в MakeClick()
3. Назначить ссылки в Inspector (префаб Insect, Target = Frog, Camera)