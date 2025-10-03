import winsound

def run():
    winsound.PlaySound("Notification.Proximity", winsound.SND_ALIAS)

if __name__ == "__main__":
    run()