from locust import HttpUser, task, between

class WebsiteUser(HttpUser):

    wait_time = between(1, 2)

    @task(3)
    def load_home(self):
        self.client.get("/home.html")

    @task(2)
    def load_second(self):
        self.client.get("/second.html")

    @task(1)
    def load_myc(self):
        self.client.get("/myc.html")

    @task(4)
    def load_pamparam(self):
        self.client.get("/pamparam.html")
